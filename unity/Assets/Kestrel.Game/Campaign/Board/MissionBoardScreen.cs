// Mission board. A vertical constellation over a pinned glass sheet.
// The path ends above the sheet, so a node is never covered. Wide WebGL
// (WebLayoutMode) puts the map on the left and the sheet on the right.
// Glass is a baked fill: no backdrop blur. Type is Fraunces and Space Grotesk.
// t461: the header (kicker, act title on whole-pixel line boxes, act bar, Patches) is its own rows above the map;
// the sheet is sized to the longest card and its body is a ScrollView past that; the payload is a lit 3D render.
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
        VisualElement _content, _body, _art, _pulse;
        ScrollView _cardScroll;
        PayloadThumb _artThumb;
        readonly List<MissionBoardCopy> _copies = new List<MissionBoardCopy>();
        MissionBoardFrame _frame;
        MissionBoardCopy _shownCopy;
        Label _act, _name, _customer, _orbit, _brief, _facts, _noteWhat, _noteDoes;
        Button _primary, _contracts, _sandboxButton, _patches;
        VisualElement _note;
        string _payloadWhat = "", _payloadDoes = "";
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
            homeKey.style.fontSize = 14f;
            homeKey.userData = home;
            Add(homeKey);

            _patches = new Button(() => { if (MissionBoardScreen.OpenPatches != null) MissionBoardScreen.OpenPatches(); })
            {
                name = "mission-patches",
                text = "Patches",
                tooltip = "Patches. The flights you have cleared.",
            };
            _patches.AddToClassList("mission-home");
            BoardType.Set(_patches, BoardType.GroteskBold);
            _patches.style.unityTextAlign = TextAnchor.MiddleCenter;
            _patches.style.fontSize = 13f;
            _patches.style.whiteSpace = WhiteSpace.NoWrap;
            Add(_patches);

            var kicker = new Label("CAMPAIGN") { name = "mission-kicker", pickingMode = PickingMode.Ignore };
            kicker.AddToClassList("mission-kicker");
            kicker.style.fontSize = 14f;
            kicker.style.whiteSpace = WhiteSpace.Normal;
            BoardType.Set(kicker, BoardType.GroteskBold);
            Add(kicker);

            _act = new Label("Missions") { name = "mission-act", pickingMode = PickingMode.Ignore };
            _act.AddToClassList("mission-act-name");
            BoardType.Set(_act, BoardType.FrauncesBold);
            _act.style.fontSize = 16f;
            _act.style.whiteSpace = WhiteSpace.Normal;
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

            BuildCopies(campaign);

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

            // The card body scrolls when the longest card does not fit the room the map leaves (t461: the brief and
            // the Need / Difficulty rows were clipped with no way to reach them). Contracts, Sandbox and the primary
            // stay pinned under it.
            _cardScroll = new ScrollView(ScrollViewMode.Vertical) { name = "mission-card-scroll" };
            _cardScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _cardScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            _cardScroll.touchScrollBehavior = ScrollView.TouchScrollBehavior.Clamped;
            _cardScroll.style.position = Position.Absolute;
            _cardScroll.style.overflow = Overflow.Hidden;
            _cardScroll.style.backgroundColor = new StyleColor(new Color(0f, 0f, 0f, 0f));
            _cardScroll.contentContainer.style.position = Position.Relative;
            _cardScroll.contentContainer.style.flexShrink = 0f;
            _body.Add(_cardScroll);

            var artButton = new Button(TogglePayloadNote) { name = "mission-art", text = string.Empty, tooltip = "Payload. What it is and what it does." };
            artButton.style.paddingLeft = 0f;
            artButton.style.paddingRight = 0f;
            artButton.style.paddingTop = 0f;
            artButton.style.paddingBottom = 0f;
            artButton.style.marginTop = 0f;
            artButton.style.marginBottom = 0f;
            artButton.style.marginLeft = 0f;
            artButton.style.marginRight = 0f;
            artButton.style.borderTopWidth = 0f;
            artButton.style.borderRightWidth = 0f;
            artButton.style.borderBottomWidth = 0f;
            artButton.style.borderLeftWidth = 0f;
            artButton.style.overflow = Overflow.Hidden;
            _art = artButton;
            _art.AddToClassList("mission-art");
            _art.style.backgroundColor = new StyleColor(MissionBoardPalette.Deep);
            Round(_art, 12f);
            _artThumb = PayloadArt.Element("mission-art-render", "box", (int)MissionBoardLayout.ArtMax, PayloadBackdrop.Dusk, urgent: true);
            _artThumb.style.position = Position.Absolute;
            _artThumb.style.left = 0f;
            _artThumb.style.top = 0f;
            _artThumb.style.right = 0f;
            _artThumb.style.bottom = 0f;
            _artThumb.style.width = StyleKeyword.Auto;
            _artThumb.style.height = StyleKeyword.Auto;
            _art.Add(_artThumb);
            _cardScroll.Add(_art);

            _name = Line("mission-name", "mission-act-name");
            _name.style.fontSize = MissionBoardLayout.NamePt;
            BoardType.Set(_name, BoardType.FrauncesBold);
            _customer = Line("mission-customer", "mission-fact");
            _customer.style.color = new StyleColor(MissionBoardPalette.Muted);
            _orbit = Line("mission-orbit", "mission-fact");
            _brief = Line("mission-brief", "mission-brief");
            _facts = Line("mission-facts", "mission-fact");
            _cardScroll.Add(_name);
            _cardScroll.Add(_brief);
            _cardScroll.Add(_customer);
            _cardScroll.Add(_orbit);
            _cardScroll.Add(_facts);

            _note = new VisualElement { name = "mission-payload-note" };
            _note.style.display = DisplayStyle.None;
            _note.style.position = Position.Absolute;
            _note.style.left = 8f;
            _note.style.right = 8f;
            _note.style.top = 8f;
            _note.style.paddingLeft = 10f;
            _note.style.paddingRight = 10f;
            _note.style.paddingTop = 10f;
            _note.style.paddingBottom = 10f;
            _note.style.backgroundColor = new StyleColor(new Color(0.06f, 0.09f, 0.16f, 0.96f));
            _note.style.flexDirection = FlexDirection.Column;
            _noteWhat = Line("mission-payload-what", "mission-brief");
            _noteDoes = Line("mission-payload-does", "mission-brief");
            var noteClose = new Button(() => _note.style.display = DisplayStyle.None) { name = "mission-payload-close", text = "Close" };
            noteClose.style.minHeight = 44f;
            noteClose.style.fontSize = 14f;
            noteClose.style.marginTop = 6f;
            _note.Add(_noteWhat);
            _note.Add(_noteDoes);
            _note.Add(noteClose);
            card.Add(_note);

            _contracts = new Button(SelectContracts) { name = "mission-contracts", text = "Contracts", tooltip = MissionMapCopy.ContractsBody };
            _contracts.AddToClassList("mission-entry");
            BoardType.Set(_contracts, BoardType.Grotesk);
            _contracts.style.unityTextAlign = TextAnchor.MiddleCenter;
            _contracts.style.fontSize = 14f;
            _body.Add(_contracts);

            _sandboxButton = new Button(() =>
            {
                if (MenuPrefs.SandboxUnlocked) SelectSandbox();
            })
            {
                name = "mission-sandbox",
                text = "Sandbox",
            };
            _sandboxButton.AddToClassList("mission-entry");
            BoardType.Set(_sandboxButton, BoardType.Grotesk);
            _sandboxButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            _sandboxButton.style.fontSize = 14f;
            ApplySandboxLock();
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
            var code = new Label(MissionContext.LabelFor(entry.Index))
            {
                name = "mission-node-code-" + id,
                pickingMode = PickingMode.Ignore,
            };
            code.style.position = Position.Absolute;
            code.style.left = 0f;
            code.style.right = 0f;
            code.style.bottom = 1f;
            code.style.height = 14f;
            code.style.fontSize = 10f;
            code.style.unityTextAlign = TextAnchor.MiddleCenter;
            code.style.whiteSpace = WhiteSpace.NoWrap;
            code.style.color = new StyleColor(Color.white);
            code.style.backgroundColor = new StyleColor(new Color(0.04f, 0.06f, 0.1f, 0.72f));
            BoardType.Set(code, BoardType.GroteskBold);
            node.Add(code);
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
            _payloadWhat = First(slot.Mission != null ? slot.Mission.WhatItIs : null, slot.Entry.PayloadLine);
            _payloadDoes = First(slot.Mission != null ? slot.Mission.WhatItDoes : null, slot.Entry.DoesLine);
            HidePayloadNote();
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
            _payloadWhat = _card.Briefing;
            _payloadDoes = _card.Orbit;
            HidePayloadNote();
            ShowArt("box");
            PaintCard(_card);
            Kick(true);
        }

        void ApplySandboxLock()
        {
            if (_sandboxButton == null) return;
            bool on = MenuPrefs.SandboxUnlocked;
            _sandboxButton.SetEnabled(on);
            _sandboxButton.tooltip = on
                ? "A free build on this device. No mission and no score."
                : "Sandbox is off. Turn it on in Settings.";
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
            _payloadWhat = _card.Briefing;
            _payloadDoes = "";
            HidePayloadNote();
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
            _shownCopy = MissionBoardCopy.Of(card, _act != null ? _act.text : "");
            SetLine(_name, _shownCopy.Name);
            SetLine(_customer, _shownCopy.Customer);
            SetLine(_orbit, _shownCopy.Orbit);
            SetLine(_brief, _shownCopy.Brief);
            SetLine(_facts, _shownCopy.Facts);
            _primary.text = string.IsNullOrEmpty(card.Primary) ? "Locked" : card.Primary;
            _primary.tooltip = string.IsNullOrEmpty(card.PrimaryTip) ? _primary.text : card.PrimaryTip;
            _primary.SetEnabled(card.CanGo);
            LayoutBody();
            if (_cardScroll != null) _cardScroll.scrollOffset = Vector2.zero;
        }

        /// <summary>
        /// Every card the board can show (each mission in its state, Contracts, Sandbox), so the sheet is sized once
        /// to the longest and does not jump between selections.
        /// </summary>
        void BuildCopies(Campaign campaign)
        {
            _copies.Clear();
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                int stars = slot.Record == null ? 0 : slot.Record.BestStars;
                IReadOnlyList<string> medals = slot.Record == null ? null : slot.Record.BestMedals;
                double score = slot.Record == null ? 0 : slot.Record.BestScore;
                int attempts = slot.Record == null ? 0 : slot.Record.Attempts;
                var facts = MissionBoardFacts.Of(slot.Mission, AccessOf(slot.Entry.Access), stars, medals, score, attempts, true);
                _copies.Add(MissionBoardCopy.Of(facts, ActTitle(slot.Entry.ActId)));
            }
            bool open = _map != null && _map.ShowOpenContracts;
            _copies.Add(MissionBoardCopy.Of(MissionBoardFacts.Contracts(open, MissionMapCopy.ContractsBody, MissionMapCopy.ContractsAfterCleared), ActTitle(null)));
            _copies.Add(MissionBoardCopy.Of(MissionBoardFacts.Sandbox(), ActTitle(null)));
            _copies.Add(MissionBoardCopy.Of(MissionBoardFacts.Of(null, MissionBoardAccess.Locked, 0, null, 0, 0, false), "Missions"));
        }

        string ActTitle(string actId)
        {
            if (_map == null || _map.Acts.Count == 0) return "Missions";
            for (int i = 0; i < _map.Acts.Count; i++)
                if (_map.Acts[i].Id == actId) return _map.Acts[i].Title ?? "";
            return _map.Acts[0].Title ?? "";
        }

        /// <summary>The shown card's blocks inside the body ScrollView: whole-pixel boxes from its copy.</summary>
        void LayoutBody()
        {
            var frame = _frame;
            if (frame == null || _cardScroll == null || _shownCopy == null) return;
            var sized = MissionBoardLayout.Body(frame, _shownCopy);
            if (sized.Scrolls)
            {
                // Leave the scroller its lane so it never sits on the text.
                sized = MissionBoardLayout.Body(Math.Max(120f, frame.Body.W - PathScrollbarW), frame.Body.H, _shownCopy);
            }
            frame.Sized = sized;
            PinInside(_cardScroll, frame.Body, frame.Card);
            var content = _cardScroll.contentContainer;
            content.style.width = frame.Body.W - (sized.Scrolls ? PathScrollbarW : 0f);
            content.style.height = sized.ContentH;
            content.style.minHeight = sized.ContentH;
            Pin(_art, sized.Art);
            TextBox(_name, sized.Name, MissionBoardLayout.NamePt);
            TextBox(_customer, sized.Customer, MissionBoardLayout.BodyPt);
            TextBox(_orbit, sized.Orbit, MissionBoardLayout.BodyPt);
            TextBox(_brief, sized.Brief, MissionBoardLayout.BodyPt);
            TextBox(_facts, sized.Facts, MissionBoardLayout.BodyPt);
            _cardScroll.verticalScrollerVisibility = sized.Scrolls ? ScrollerVisibility.Auto : ScrollerVisibility.Hidden;
        }

        /// <summary>A label pinned to a whole-pixel box of its line boxes: no theme padding or margin.</summary>
        static void TextBox(Label label, HudBox box, float pt)
        {
            if (label == null) return;
            Pin(label, box);
            Unpad(label);
            label.style.minHeight = box.H;
            label.style.fontSize = pt;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.unityTextAlign = TextAnchor.UpperLeft;
            label.style.overflow = Overflow.Hidden;
            if (box.H < 1f) label.style.display = DisplayStyle.None;
            else if (!string.IsNullOrEmpty(label.text)) label.style.display = DisplayStyle.Flex;
        }

        static void Unpad(VisualElement el)
        {
            el.style.paddingLeft = 0f;
            el.style.paddingRight = 0f;
            el.style.paddingTop = 0f;
            el.style.paddingBottom = 0f;
            el.style.marginLeft = 0f;
            el.style.marginRight = 0f;
            el.style.marginTop = 0f;
            el.style.marginBottom = 0f;
        }

        static string Join(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b ?? "";
            if (string.IsNullOrEmpty(b)) return a;
            return a + "  ·  " + b;
        }

        void ShowArt(string form)
        {
            _artThumb?.SetForm(form, PayloadBackdrop.Dusk);
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
                _primary.EnableInClassList("mission-fly--down", false);
                _primary.style.top = _primaryTop;
                return;
            }
            if (_reduce || !_primary.enabledSelf) return;
            _down = true;
            // Moving the button on pointer-down slides it out from under the finger, so the
            // first tap never fires clicked. The press is a class, not a new top.
            _primary.EnableInClassList("mission-fly--down", true);
        }

        void ApplyFromRoot()
        {
            float rw = resolvedStyle != null ? resolvedStyle.width : 0f;
            float rh = resolvedStyle != null ? resolvedStyle.height : 0f;
            bool laidOut = rw > 2f && rh > 2f;
            float w;
            float h;
            bool wide;
            if (_previewW > 1f && _previewH > 1f && !laidOut)
            {
                w = _previewW;
                h = _previewH;
                wide = _wideOverride ?? WebLayoutMode.UseWide(w, h, true);
            }
            else if (laidOut)
            {
                // The phone column is narrower than the device preview. Lay the sheet out in the
                // box this view actually has, or the primary sits past the viewport.
                w = rw;
                h = rh;
                if (_wideOverride.HasValue) wide = _wideOverride.Value;
                else if (_previewW > 1f) wide = WebLayoutMode.UseWide(_previewW, _previewH, true);
                else wide = WebLayoutMode.UseWide(Screen.width, Screen.height);
            }
            else return;
            if (wide) ReleaseColumnCap();
            Apply(w, h, wide);
        }

        /// <summary>.kh-screen caps the host at 600. A wide board needs the safe width, same as Home.</summary>
        void ReleaseColumnCap()
        {
            var host = parent;
            if (host == null || host.ClassListContains("kh-wide-open")) return;
            if (!host.ClassListContains("kh-screen") && !host.ClassListContains("kh-home")) return;
            host.AddToClassList("kh-wide-open");
            host.style.maxWidth = Length.Percent(100);
            host.style.alignSelf = Align.Stretch;
        }

        void Apply(float width, float height, bool wide)
        {
            WebLayoutMode.ExitInset(WebLayoutMode.WantExitReserve, out float safeTop, out float safeRight);
            var frame = MissionBoardLayout.Place(width, height, safeTop, 0f, 0f, safeRight, wide, _copies.Count > 0 ? _copies : null);
            _frame = frame;
            userData = frame;
            EnableInClassList("mission-board--wide", wide);

            var home = this.Q("mission-home");
            var kicker = this.Q("mission-kicker");
            Pin(home, frame.Home);
            if (kicker != null)
            {
                Pin(kicker, frame.Kicker);
                Unpad(kicker);
                kicker.style.minHeight = frame.Kicker.H;
                kicker.style.fontSize = Kestrel.Game.Shell.TextFit.Cap(kicker, MissionBoardLayout.KickerPt);
                kicker.style.whiteSpace = WhiteSpace.Normal;
                kicker.style.unityTextAlign = TextAnchor.MiddleLeft;
            }
            if (_act != null)
            {
                // Whole-pixel line boxes: ActLines x ceil(font x 1.3), no theme padding (t461: a 32 pt box for two
                // 16 pt lines, under the act bar).
                Pin(_act, frame.Act);
                Unpad(_act);
                _act.style.minHeight = frame.Act.H;
                _act.style.fontSize = Kestrel.Game.Shell.TextFit.Cap(_act, frame.ActFontPt);
                _act.style.whiteSpace = WhiteSpace.Normal;
                _act.style.unityTextAlign = TextAnchor.UpperLeft;
            }
            if (_patches != null)
            {
                // Its own header slot (t461: it sat inside the map on the KH-2 node).
                Pin(_patches, frame.Patches);
                _patches.style.marginLeft = 0f;
                _patches.style.marginRight = 0f;
                _patches.style.marginTop = 0f;
                _patches.style.marginBottom = 0f;
                _patches.style.whiteSpace = WhiteSpace.NoWrap;
                _patches.style.fontSize = Kestrel.Game.Shell.TextFit.Cap(_patches, 13f);
            }
            for (int i = 0; i < _segments.Length; i++)
                Pin(_segments[i], MissionBoardLayout.Segment(frame.Progress, i, MissionBoardLayout.ActSlots));

            Pin(_path, frame.Path);
            _patches?.BringToFront();
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
            LayoutBody();
            PinInside(_contracts, frame.Contracts, frame.Card);
            PinInside(_sandboxButton, frame.Sandbox, frame.Card);
            PinInside(_primary, frame.Primary, frame.Card);
            _primaryTop = frame.Primary.Y - frame.Card.Y;
            if (!_down) _primary.style.top = _primaryTop;
            ApplyMotion();
            Kestrel.Game.Shell.TextFit.Apply(this);
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
            // Time-sliced payload renders (a few ms a frame).
            PayloadThumbnails.PumpFrame();
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

        void TogglePayloadNote()
        {
            if (_note == null) return;
            if (_note.style.display.value != DisplayStyle.None)
            {
                HidePayloadNote();
                return;
            }
            string what = string.IsNullOrEmpty(_payloadWhat) ? "The payload rides this flight." : _payloadWhat;
            string does = _payloadDoes ?? "";
            SetLine(_noteWhat, what);
            SetLine(_noteDoes, does);
            _note.style.display = DisplayStyle.Flex;
            _note.BringToFront();
        }

        void HidePayloadNote()
        {
            if (_note != null) _note.style.display = DisplayStyle.None;
        }

        static string First(string a, string b)
        {
            if (!string.IsNullOrWhiteSpace(a)) return a.Trim();
            return string.IsNullOrWhiteSpace(b) ? "" : b.Trim();
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
            label.style.fontSize = 14f;
            BoardType.Set(label, BoardType.Grotesk);
            return label;
        }

        static void SetLine(Label label, string text)
        {
            if (label == null) return;
            label.text = text ?? "";
            label.style.display = string.IsNullOrEmpty(label.text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Room for the path ScrollView's vertical bar (Unity's runtime theme draws it about 13 px wide).</summary>
        public const float PathScrollbarW = 16f;

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

        /// <summary>Home wires this to the patch collection. The board button does nothing until it is set.</summary>
        public static Action OpenPatches;

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
