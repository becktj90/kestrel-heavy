// Mission board. A vertical constellation over a pinned glass sheet.
// The path ends above the sheet, so a node is never covered. Wide WebGL
// (WebLayoutMode) puts the map on the left and the sheet on the right.
// Glass is a baked fill: no backdrop blur. Type is Fraunces and Space Grotesk.
using System;
using System.Collections.Generic;
using System.IO;
using Kestrel.Core.Data;
using Kestrel.Core.Economy;
using Kestrel.Core.Flight;
using Kestrel.Game.Guide;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kestrel.Game.MissionSelect
{
    public enum MissionBoardMode { Mission, Contracts, Sandbox }

    /// <summary>The live mission-select tree. Positions come from <see cref="MissionBoardLayout"/>.</summary>
    public sealed class MissionBoardView : VisualElement
    {
        public const string RootName = "mission-board";

        readonly List<Slot> _slots = new List<Slot>();
        readonly Label[] _segments = new Label[MissionBoardLayout.ActSlots];
        readonly bool _reduce;
        readonly bool? _wideOverride;
        readonly float _previewW, _previewH;
        Action<string> _launch, _design;
        Action _sandbox;
        Func<string, bool> _canLaunch;

        MissionMap _map;
        ScrollView _path;
        VisualElement _content, _body, _copy, _art, _pulse;
        Label _act, _name, _customer, _orbit, _brief, _facts;
        Button _primary, _contracts, _sandboxButton;
        MissionTrailElement _trail;
        VisualElement _pendingScroll;
        Slot _selected;
        MissionBoardFacts _card;
        MissionBoardMode _mode = MissionBoardMode.Mission;
        string _actId;
        int _goldThrough;
        float _shown = 1f;
        float _pulsePhase;
        float _primaryTop;
        bool _clock;
        bool _scrollArmed;
        bool _down;

        MissionBoardView(bool reduce, float previewW, float previewH, bool? wide)
        {
            _reduce = reduce;
            _previewW = previewW;
            _previewH = previewH;
            _wideOverride = wide;
            name = RootName;
            AddToClassList(RootName);
            if (reduce) AddToClassList("mission-board--reduce");
            pickingMode = PickingMode.Position;
            style.flexGrow = 1f;
            style.position = Position.Relative;
            style.overflow = Overflow.Hidden;
        }

        public static MissionBoardView Create(
            Campaign campaign,
            Career career,
            IReadOnlyDictionary<string, MissionRecord> records,
            IReadOnlyDictionary<string, PayloadLabel> labels,
            Action<string> launch,
            Action<string> design,
            Action sandbox,
            Action home,
            bool reduceMotion,
            Func<string, bool> canLaunch,
            float previewWidth,
            float previewHeight,
            bool? wide)
        {
            var view = new MissionBoardView(reduceMotion, previewWidth, previewHeight, wide);
            view.Build(campaign, career, records, labels, launch, design, sandbox, home, canLaunch);
            return view;
        }

        void Build(
            Campaign campaign,
            Career career,
            IReadOnlyDictionary<string, MissionRecord> records,
            IReadOnlyDictionary<string, PayloadLabel> labels,
            Action<string> launch,
            Action<string> design,
            Action sandbox,
            Action home,
            Func<string, bool> canLaunch)
        {
            _launch = launch;
            _design = design;
            _sandbox = sandbox;
            // canLaunch is read on selection, not every frame.
            _canLaunch = canLaunch;
            _map = MissionMapBuilder.From(campaign, career, records, labels);

            var sheet = Resources.Load<StyleSheet>(MissionBoardScreen.SheetResource);
            if (sheet != null) styleSheets.Add(sheet);

            var sky = new MissionSkyElement();
            sky.pickingMode = PickingMode.Ignore;
            sky.style.position = Position.Absolute;
            sky.style.left = 0f;
            sky.style.top = 0f;
            sky.style.right = 0f;
            sky.style.bottom = 0f;
            Add(sky);

            var homeKey = new Button(home) { name = "mission-home", text = MissionMapCopy.Home, tooltip = MissionMapCopy.HomeTip };
            homeKey.AddToClassList("mission-home");
            BoardType.Set(homeKey, BoardType.GroteskBold);
            homeKey.style.unityTextAlign = TextAnchor.MiddleCenter;
            Add(homeKey);

            var kicker = new Label("CAMPAIGN") { name = "mission-kicker", pickingMode = PickingMode.Ignore };
            kicker.AddToClassList("mission-kicker");
            BoardType.Set(kicker, BoardType.GroteskBold);
            Add(kicker);

            _act = new Label("Missions") { name = "mission-act", pickingMode = PickingMode.Ignore };
            _act.AddToClassList("mission-act-name");
            BoardType.Set(_act, BoardType.FrauncesBold);
            _act.style.fontSize = 18f;
            _act.style.whiteSpace = WhiteSpace.NoWrap;
            _act.style.overflow = Overflow.Hidden;
            Add(_act);

            for (int i = 0; i < _segments.Length; i++)
            {
                var seg = new Label { name = "mission-progress-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), pickingMode = PickingMode.Ignore };
                seg.AddToClassList("mission-progress-off");
                _segments[i] = seg;
                Add(seg);
            }

            _path = new ScrollView(ScrollViewMode.Vertical) { name = "mission-path" };
            _path.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _path.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _path.style.overflow = Overflow.Hidden;
            _path.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
            Add(_path);

            _content = new VisualElement { name = "mission-constellation", pickingMode = PickingMode.Ignore };
            _content.style.position = Position.Relative;
            _content.style.flexGrow = 0f;
            _content.style.flexShrink = 0f;
            _path.Add(_content);

            _trail = new MissionTrailElement();
            _trail.style.position = Position.Absolute;
            _trail.style.left = 0f;
            _trail.style.top = 0f;
            _trail.style.right = 0f;
            _trail.style.bottom = 0f;
            _content.Add(_trail);

            var ordered = new List<MissionMapEntry>();
            for (int a = 0; a < _map.Acts.Count; a++)
                for (int e = 0; e < _map.Acts[a].Entries.Count; e++)
                    ordered.Add(_map.Acts[a].Entries[e]);

            _goldThrough = ordered.Count;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].IsNext) { _goldThrough = i; break; }
            }

            for (int i = 0; i < ordered.Count; i++)
            {
                var entry = ordered[i];
                MissionRecord record = null;
                if (records != null && entry.MissionId != null) records.TryGetValue(entry.MissionId, out record);
                CampaignMission mission = campaign != null ? campaign.ById(entry.MissionId) : null;
                if (mission == null && campaign != null) mission = campaign.Mission(entry.Index);
                _slots.Add(MakeSlot(entry, mission, record));
            }

            if (_map.Empty)
            {
                var note = new Label(string.IsNullOrEmpty(_map.Note) ? MissionMapCopy.Empty : _map.Note)
                {
                    name = "mission-empty",
                    pickingMode = PickingMode.Ignore,
                };
                note.AddToClassList("mission-brief");
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.marginLeft = 8f;
                note.style.marginTop = 8f;
                BoardType.Set(note, BoardType.Grotesk);
                _content.Add(note);
            }

            var card = new VisualElement { name = "mission-card" };
            card.AddToClassList("mission-sheet");
            card.style.overflow = Overflow.Hidden;
            Add(card);

            _body = new VisualElement { name = "mission-sheet-body", pickingMode = PickingMode.Ignore };
            _body.style.position = Position.Absolute;
            _body.style.left = 0f;
            _body.style.right = 0f;
            _body.style.top = 0f;
            card.Add(_body);

            _art = new VisualElement { name = "mission-art", pickingMode = PickingMode.Ignore };
            _art.AddToClassList("mission-art");
            _art.style.backgroundColor = new StyleColor(MissionBoardPalette.Deep);
            Round(_art, 12f);
            _body.Add(_art);

            _copy = new VisualElement { name = "mission-copy", pickingMode = PickingMode.Ignore };
            _copy.style.position = Position.Absolute;
            _copy.style.overflow = Overflow.Hidden;
            _copy.style.flexDirection = FlexDirection.Column;
            _body.Add(_copy);

            _name = Line("mission-name", "mission-act-name");
            _name.style.fontSize = 20f;
            BoardType.Set(_name, BoardType.FrauncesBold);
            _customer = Line("mission-customer", "mission-fact");
            _customer.style.color = new StyleColor(MissionBoardPalette.Muted);
            _orbit = Line("mission-orbit", "mission-fact");
            _brief = Line("mission-brief", "mission-brief");
            _facts = Line("mission-facts", "mission-fact");
            _copy.Add(_name);
            _copy.Add(_customer);
            _copy.Add(_orbit);
            _copy.Add(_brief);
            _body.Add(_facts);

            _contracts = new Button(SelectContracts) { name = "mission-contracts", text = "Contracts", tooltip = MissionMapCopy.ContractsBody };
            _contracts.AddToClassList("mission-entry");
            BoardType.Set(_contracts, BoardType.Grotesk);
            _contracts.style.unityTextAlign = TextAnchor.MiddleCenter;
            _body.Add(_contracts);

            _sandboxButton = new Button(SelectSandbox) { name = "mission-sandbox", text = "Sandbox", tooltip = "A free build on this device. No mission and no score." };
            _sandboxButton.AddToClassList("mission-entry");
            BoardType.Set(_sandboxButton, BoardType.Grotesk);
            _sandboxButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            _body.Add(_sandboxButton);

            _primary = new Button(OnPrimary) { name = "mission-primary", text = "Locked" };
            _primary.AddToClassList("mission-fly");
            BoardType.Set(_primary, BoardType.FrauncesBlack);
            _primary.style.unityTextAlign = TextAnchor.MiddleCenter;
            _primary.RegisterCallback<PointerDownEvent>(_ => Nudge(true));
            _primary.RegisterCallback<PointerUpEvent>(_ => Nudge(false));
            _primary.RegisterCallback<PointerLeaveEvent>(_ => Nudge(false));
            _primary.RegisterCallback<PointerCancelEvent>(_ => Nudge(false));
            _body.Add(_primary);

            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                ApplyFromRoot();
                QueueScroll(null);
            });
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ArmClock();
                QueueScroll(_pendingScroll);
            });

            if (_map.Next != null) SelectMission(_map.Next.MissionId, false);
            else if (_slots.Count > 0) SelectMission(_slots[0].Entry.MissionId, false);
            else
            {
                _card = MissionBoardFacts.Of(null, MissionBoardAccess.Locked, 0, null, 0, 0, false);
                PaintCard(_card);
                _act.text = "Missions";
            }

            if (_previewW > 1f && _previewH > 1f) ApplyFromRoot();
            ArmClock();
            GuideHooks.MissionSelect(GuideSession.Engine, _map.Next != null ? _map.Next.MissionId : null, _map.ShowOpenContracts);
        }

        Slot MakeSlot(MissionMapEntry entry, CampaignMission mission, MissionRecord record)
        {
            string id = string.IsNullOrEmpty(entry.MissionId) ? "i" + entry.Index.ToString(System.Globalization.CultureInfo.InvariantCulture) : entry.MissionId;
            var slot = new Slot { Entry = entry, Mission = mission, Record = record };
            var wrap = new VisualElement { name = "mission-node-wrap-" + id, pickingMode = PickingMode.Ignore };
            wrap.style.position = Position.Absolute;
            if (entry.Access == MissionAccess.Locked) wrap.AddToClassList("mission-node--locked");

            if (entry.IsNext)
            {
                _pulse = new VisualElement { name = "mission-pulse", pickingMode = PickingMode.Ignore };
                _pulse.style.position = Position.Absolute;
                _pulse.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
                _pulse.style.borderTopWidth = 2f;
                _pulse.style.borderRightWidth = 2f;
                _pulse.style.borderBottomWidth = 2f;
                _pulse.style.borderLeftWidth = 2f;
                _pulse.style.borderTopColor = new StyleColor(MissionBoardPalette.Accent);
                _pulse.style.borderRightColor = new StyleColor(MissionBoardPalette.Accent);
                _pulse.style.borderBottomColor = new StyleColor(MissionBoardPalette.Accent);
                _pulse.style.borderLeftColor = new StyleColor(MissionBoardPalette.Accent);
                _pulse.style.opacity = _reduce ? 0.8f : 0.7f;
                wrap.Add(_pulse);
            }

            var node = new Button(() => SelectMission(entry.MissionId, true))
            {
                name = "mission-node-" + id,
                text = "",
                tooltip = entry.Tooltip,
            };
            node.AddToClassList("mission-node");
            if (entry.Access == MissionAccess.Cleared) node.AddToClassList("mission-node--cleared");
            else if (entry.Access == MissionAccess.Open) node.AddToClassList("mission-node--open");
            float d = MissionBoardLayout.NodeR * 2f;
            node.style.position = Position.Absolute;
            node.style.left = 0f;
            node.style.top = 0f;
            node.style.width = d;
            node.style.height = d;
            node.style.overflow = Overflow.Hidden;
            node.style.paddingLeft = 0f;
            node.style.paddingRight = 0f;
            node.style.paddingTop = 0f;
            node.style.paddingBottom = 0f;
            Round(node, MissionBoardLayout.NodeR);
            int art = (int)(d - 8f);
            if (art < 32) art = 32;
            node.Add(PayloadArt.Element("mission-node-art-" + id, entry.Form, art));
            wrap.Add(node);
            slot.Node = node;

            if (entry.Access == MissionAccess.Cleared)
            {
                var row = new VisualElement { name = "mission-stars-" + id, pickingMode = PickingMode.Ignore };
                row.style.position = Position.Absolute;
                row.style.left = 0f;
                row.style.top = d + 1f;
                row.style.width = d;
                row.style.height = MissionBoardLayout.StarRow;
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.Center;
                int earned = record == null ? 0 : record.BestStars;
                if (earned < 0) earned = 0;
                if (earned > 3) earned = 3;
                slot.Stars = new MissionStarPip[3];
                for (int s = 0; s < 3; s++)
                {
                    var pip = new MissionStarPip { On = s < earned };
                    pip.style.width = 10f;
                    pip.style.height = 10f;
                    pip.style.marginLeft = 1f;
                    pip.style.marginRight = 1f;
                    slot.Stars[s] = pip;
                    row.Add(pip);
                }
                wrap.Add(row);
            }

            slot.Wrap = wrap;
            _content.Add(wrap);
            return slot;
        }

        public void SelectMission(string missionId, bool animate)
        {
            Slot slot = null;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Entry.MissionId == missionId) { slot = _slots[i]; break; }
            }
            if (slot == null) return;
            _mode = MissionBoardMode.Mission;
            _selected = slot;
            _actId = slot.Entry.ActId;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].Node.EnableInClassList("mission-node--selected", _slots[i] == slot);
            _contracts.EnableInClassList("mission-entry--on", false);
            _sandboxButton.EnableInClassList("mission-entry--on", false);
            PaintAct(_actId);
            bool can = slot.Mission != null && _canLaunch != null && slot.Mission.Id != null && _canLaunch(slot.Mission.Id);
            int stars = slot.Record == null ? 0 : slot.Record.BestStars;
            IReadOnlyList<string> medals = slot.Record == null ? null : slot.Record.BestMedals;
            double score = slot.Record == null ? 0 : slot.Record.BestScore;
            int attempts = slot.Record == null ? 0 : slot.Record.Attempts;
            _card = MissionBoardFacts.Of(slot.Mission, AccessOf(slot.Entry.Access), stars, medals, score, attempts, can);
            ShowArt(slot.Entry.Form);
            PaintCard(_card);
            Kick(animate);
            QueueScroll(slot.Node);
        }

        public void SelectContracts()
        {
            _mode = MissionBoardMode.Contracts;
            _selected = null;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].Node.EnableInClassList("mission-node--selected", false);
            _contracts.EnableInClassList("mission-entry--on", true);
            _sandboxButton.EnableInClassList("mission-entry--on", false);
            _card = MissionBoardFacts.Contracts(_map != null && _map.ShowOpenContracts, MissionMapCopy.ContractsBody, MissionMapCopy.ContractsAfterCleared);
            ShowArt("box");
            PaintCard(_card);
            Kick(true);
        }

        public void SelectSandbox()
        {
            _mode = MissionBoardMode.Sandbox;
            _selected = null;
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].Node.EnableInClassList("mission-node--selected", false);
            _contracts.EnableInClassList("mission-entry--on", false);
            _sandboxButton.EnableInClassList("mission-entry--on", true);
            _card = MissionBoardFacts.Sandbox();
            ShowArt("box");
            PaintCard(_card);
            Kick(true);
        }

        void OnPrimary()
        {
            if (_card == null || !_card.CanGo) return;
            if (_mode == MissionBoardMode.Sandbox)
            {
                _sandbox?.Invoke();
                return;
            }
            if (_mode != MissionBoardMode.Mission || _selected == null || _selected.Mission == null) return;
            string id = _selected.Mission.Id;
            if (!string.IsNullOrEmpty(_card.Primary) && _card.Primary.StartsWith("Fly ", StringComparison.Ordinal))
                _launch?.Invoke(id);
            else
                _design?.Invoke(id);
        }

        void PaintAct(string actId)
        {
            MissionAct act = null;
            if (_map != null)
            {
                for (int i = 0; i < _map.Acts.Count; i++)
                {
                    if (_map.Acts[i].Id == actId) { act = _map.Acts[i]; break; }
                }
                if (act == null && _map.Acts.Count > 0) act = _map.Acts[0];
            }
            _act.text = act != null ? act.Title : "Missions";
            for (int i = 0; i < _segments.Length; i++)
            {
                bool on = act != null && i < act.Entries.Count && act.Entries[i].Access == MissionAccess.Cleared;
                _segments[i].EnableInClassList("mission-progress-on", on);
                _segments[i].EnableInClassList("mission-progress-off", !on);
            }
        }

        void PaintCard(MissionBoardFacts card)
        {
            if (card == null) card = MissionBoardFacts.Of(null, MissionBoardAccess.Locked, 0, null, 0, 0, false);
            _card = card;
            SetLine(_name, card.Title);
            SetLine(_customer, card.Customer);
            SetLine(_orbit, card.Orbit);
            SetLine(_brief, card.Briefing);
            SetLine(_facts, FactsText(card));
            _primary.text = string.IsNullOrEmpty(card.Primary) ? "Locked" : card.Primary;
            _primary.tooltip = string.IsNullOrEmpty(card.PrimaryTip) ? _primary.text : card.PrimaryTip;
            _primary.SetEnabled(card.CanGo);
        }

        static string FactsText(MissionBoardFacts card)
        {
            string top = Join(card.Need, card.Mass);
            string bottom = Join(card.Difficulty, card.Best);
            if (top.Length == 0) return bottom;
            if (bottom.Length == 0) return top;
            return top + "\n" + bottom;
        }

        static string Join(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b ?? "";
            if (string.IsNullOrEmpty(b)) return a;
            return a + "  ·  " + b;
        }

        void ShowArt(string form)
        {
            var tex = PayloadArt.Texture(form, 128);
            if (tex != null) _art.style.backgroundImage = new StyleBackground(tex);
        }

        void Kick(bool animate)
        {
            _shown = animate && !_reduce ? 0f : 1f;
            _down = false;
            ApplyMotion();
        }

        void ApplyMotion()
        {
            if (_body == null) return;
            _body.style.opacity = MissionBoardMotion.Blend(_shown, _reduce);
            _body.style.top = MissionBoardMotion.Slide(_shown, _reduce);
        }

        void Nudge(bool down)
        {
            if (_primary == null) return;
            if (!down)
            {
                _down = false;
                _primary.style.top = _primaryTop;
                return;
            }
            if (_reduce || !_primary.enabledSelf) return;
            _down = true;
            _primary.style.top = _primaryTop + 4f;
        }

        void ApplyFromRoot()
        {
            float w;
            float h;
            bool wide;
            if (_previewW > 1f && _previewH > 1f)
            {
                w = _previewW;
                h = _previewH;
                wide = _wideOverride ?? WebLayoutMode.UseWide(w, h, true);
            }
            else
            {
                w = resolvedStyle != null ? resolvedStyle.width : 0f;
                h = resolvedStyle != null ? resolvedStyle.height : 0f;
                if (w < 2f || h < 2f) return;
                // The view sits inside the shell safe area, so the frame insets stay 0.
                // UseWide() is false outside the WebGL player, which keeps iOS in the phone column.
                wide = _wideOverride ?? WebLayoutMode.UseWide(Screen.width, Screen.height);
            }
            Apply(w, h, wide);
        }

        void Apply(float width, float height, bool wide)
        {
            var frame = MissionBoardLayout.Place(width, height, 0f, 0f, 0f, 0f, wide);
            userData = frame;
            EnableInClassList("mission-board--wide", wide);

            var home = this.Q("mission-home");
            var kicker = this.Q("mission-kicker");
            Pin(home, frame.Home);
            if (kicker != null)
            {
                kicker.style.position = Position.Absolute;
                kicker.style.left = frame.Title.X;
                kicker.style.top = frame.Title.Y;
                kicker.style.width = frame.Title.W;
                kicker.style.height = 12f;
            }
            if (_act != null)
            {
                _act.style.position = Position.Absolute;
                _act.style.left = frame.Title.X;
                _act.style.top = frame.Title.Y + 12f;
                _act.style.width = frame.Title.W;
                _act.style.height = Math.Max(14f, frame.Title.H - 12f);
            }
            for (int i = 0; i < _segments.Length; i++)
                Pin(_segments[i], MissionBoardLayout.Segment(frame.Progress, i, MissionBoardLayout.ActSlots));

            Pin(_path, frame.Path);
            var card = this.Q("mission-card");
            Pin(card, frame.Card);

            float contentH = MissionBoardLayout.ContentHeight(_slots.Count);
            _content.style.width = frame.Path.W;
            _content.style.height = contentH;
            var nodes = MissionBoardLayout.Nodes(frame.Path.W, _slots.Count);
            _trail.Refresh(nodes, _goldThrough);
            float d = MissionBoardLayout.NodeR * 2f;
            float extra = 6f;
            for (int i = 0; i < _slots.Count && i < nodes.Length; i++)
            {
                var wrap = _slots[i].Wrap;
                wrap.style.left = nodes[i].X - MissionBoardLayout.NodeR;
                wrap.style.top = nodes[i].Y - MissionBoardLayout.NodeR;
                wrap.style.width = d;
                wrap.style.height = d + MissionBoardLayout.StarRow;
            }
            if (_pulse != null)
            {
                _pulse.style.left = -extra * 0.5f;
                _pulse.style.top = -extra * 0.5f;
                _pulse.style.width = d + extra;
                _pulse.style.height = d + extra;
                Round(_pulse, (d + extra) * 0.5f);
            }

            if (_body != null)
            {
                _body.style.width = frame.Card.W;
                _body.style.height = frame.Card.H;
            }
            PinInside(_art, frame.Art, frame.Card);
            if (_copy != null)
            {
                float top = frame.Name.Y - frame.Card.Y;
                float bottom = frame.Facts.Y - frame.Card.Y;
                _copy.style.left = frame.Name.X - frame.Card.X;
                _copy.style.top = top;
                _copy.style.width = frame.Name.W;
                _copy.style.height = Math.Max(16f, bottom - top);
            }
            PinInside(_facts, frame.Facts, frame.Card);
            PinInside(_contracts, frame.Contracts, frame.Card);
            PinInside(_sandboxButton, frame.Sandbox, frame.Card);
            PinInside(_primary, frame.Primary, frame.Card);
            _primaryTop = frame.Primary.Y - frame.Card.Y;
            if (!_down) _primary.style.top = _primaryTop;
            ApplyMotion();
        }

        /// <summary>
        /// ScrollView.ScrollTo throws if the list is not on a panel yet. Build runs before
        /// the shell parents this view, so the scroll waits until attach.
        /// </summary>
        void QueueScroll(VisualElement node)
        {
            if (node != null) _pendingScroll = node;
            if (_pendingScroll == null || _path == null || panel == null || _scrollArmed) return;
            var sched = schedule;
            if (sched == null)
            {
                SafeScroll();
                return;
            }
            _scrollArmed = true;
            sched.Execute(() =>
            {
                _scrollArmed = false;
                SafeScroll();
            });
        }

        void SafeScroll()
        {
            var node = _pendingScroll;
            if (node == null || _path == null || _path.panel == null) return;
            try
            {
                _path.ScrollTo(node);
                _pendingScroll = null;
            }
            catch (NullReferenceException)
            {
                // Unity's ScrollTo reads the panel before the scroller exists.
            }
        }

        void ArmClock()
        {
            if (_clock || schedule == null) return;
            var item = schedule.Execute(Tick);
            if (item == null) return;
            item.Every(16);
            _clock = true;
        }

        void Tick()
        {
            if (_shown < 1f)
            {
                _shown += 0.016f / MissionBoardMotion.Seconds;
                if (_shown > 1f) _shown = 1f;
                ApplyMotion();
            }
            if (_pulse == null) return;
            if (_reduce)
            {
                _pulse.style.opacity = 0.8f;
                return;
            }
            _pulsePhase += 0.016f / 1.2f;
            if (_pulsePhase > 1f) _pulsePhase -= 1f;
            float tri = _pulsePhase < 0.5f ? _pulsePhase * 2f : (1f - _pulsePhase) * 2f;
            _pulse.style.opacity = 0.25f + 0.75f * tri;
        }

        static MissionBoardAccess AccessOf(MissionAccess access)
        {
            if (access == MissionAccess.Locked) return MissionBoardAccess.Locked;
            if (access == MissionAccess.Cleared) return MissionBoardAccess.Cleared;
            return MissionBoardAccess.Open;
        }

        static Label Line(string elementName, string cls)
        {
            var label = new Label { name = elementName, pickingMode = PickingMode.Ignore };
            label.AddToClassList(cls);
            label.style.whiteSpace = WhiteSpace.Normal;
            BoardType.Set(label, BoardType.Grotesk);
            return label;
        }

        static void SetLine(Label label, string text)
        {
            if (label == null) return;
            label.text = text ?? "";
            label.style.display = string.IsNullOrEmpty(label.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        static void Pin(VisualElement el, HudBox box)
        {
            if (el == null) return;
            el.style.position = Position.Absolute;
            el.style.left = box.X;
            el.style.top = box.Y;
            el.style.width = box.W;
            el.style.height = box.H;
        }

        static void PinInside(VisualElement el, HudBox box, HudBox parent)
        {
            if (el == null) return;
            el.style.position = Position.Absolute;
            el.style.left = box.X - parent.X;
            el.style.top = box.Y - parent.Y;
            el.style.width = box.W;
            el.style.height = box.H;
        }

        static void Round(VisualElement el, float radius)
        {
            el.style.borderTopLeftRadius = radius;
            el.style.borderTopRightRadius = radius;
            el.style.borderBottomLeftRadius = radius;
            el.style.borderBottomRightRadius = radius;
        }

        sealed class Slot
        {
            public MissionMapEntry Entry;
            public CampaignMission Mission;
            public MissionRecord Record;
            public VisualElement Wrap;
            public Button Node;
            public MissionStarPip[] Stars;
        }
    }

    public static class MissionBoardScreen
    {
        public const string SheetResource = "UI/MissionBoard";

        public static MissionBoardView Build(
            Campaign campaign,
            Action<string> launch,
            Action<string> design,
            Action sandbox,
            Action home,
            bool reduceMotion,
            Func<string, bool> canLaunch)
        {
            string json = ReadCampaignText();
            var labels = MissionMapText.ReadPayloadLabels(json);
            long minor = MissionMapText.MinorPerKc(json);
            MissionSnapshot snap;
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "saves");
                snap = MissionProgress.ReadSave(campaign, dir, minor);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Kestrel] career unreadable, showing a fresh board: " + e.Message);
                snap = campaign == null ? new MissionSnapshot() : MissionProgress.Fresh(campaign, minor);
            }
            return MissionBoardView.Create(campaign, snap.Career, snap.Records, labels, launch, design, sandbox, home, reduceMotion, canLaunch, 0f, 0f, null);
        }

        public static MissionBoardView Build(
            Campaign campaign,
            Career career,
            IReadOnlyDictionary<string, MissionRecord> records,
            IReadOnlyDictionary<string, PayloadLabel> labels,
            Action<string> launch,
            Action<string> design,
            Action sandbox,
            Action home,
            bool reduceMotion,
            Func<string, bool> canLaunch,
            float previewWidth,
            float previewHeight,
            bool? wide)
        {
            return MissionBoardView.Create(campaign, career, records, labels, launch, design, sandbox, home, reduceMotion, canLaunch, previewWidth, previewHeight, wide);
        }

        static string ReadCampaignText()
        {
            try
            {
                string root = Catalog.RepoRootOrNull();
                if (!string.IsNullOrEmpty(root))
                {
                    string path = Path.Combine(root, "data", "campaign.json");
                    if (File.Exists(path)) return File.ReadAllText(path);
                }
            }
            catch (Exception) { }
            var asset = Resources.Load<TextAsset>("KestrelData/campaign");
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text)) return asset.text;
            return null;
        }
    }

    static class BoardType
    {
        public const int FrauncesBlack = 0;
        public const int FrauncesBold = 1;
        public const int GroteskBold = 2;
        public const int Grotesk = 3;

        static bool _loaded;
        static Font _black, _bold, _groBold, _gro;

        public static void Set(VisualElement el, int which)
        {
            if (el == null) return;
            if (!_loaded)
            {
                _loaded = true;
                _black = Resources.Load<Font>("KHFonts/Fraunces-Black");
                _bold = Resources.Load<Font>("KHFonts/Fraunces-Bold");
                _groBold = Resources.Load<Font>("KHFonts/SpaceGrotesk-Bold");
                _gro = Resources.Load<Font>("KHFonts/SpaceGrotesk-Medium");
            }
            Font font = which == FrauncesBlack ? _black : which == FrauncesBold ? _bold : which == GroteskBold ? _groBold : _gro;
            if (font == null) return;
            el.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font));
        }
    }
}
