// The flight camera's control rules, pure so EditMode tests pin them without Unity. FlightChrome (Slice) holds the
// state and draws the buttons; this decides what a tap does.
//
// The rules (owner feedback, Oct 2026: "The camera buttons need to work better"):
//  1. The player's choice wins. Auto is a chase framing, never a director: nothing but the player changes the view,
//     with one exception (rule 4).
//  2. Follow's label always matches the state: "Following" while the camera rides with the rocket and no orbit
//     shows, "Follow rocket" otherwise. "Follow rocket" always brings the camera back (recentre the orbit, and from
//     Pad back to the last chasing view). "Following" lets go to the Pad ground camera only while that camera can
//     see the rocket; out of its range the tap changes nothing and a toast says so (it used to cut to a dot at 60 km).
//  3. A view the camera cannot use in this phase is disabled: the Pad chip is dimmed once the pad camera loses the
//     rocket, a tap on it only explains, and C skips it.
//  4. If the player is on Pad when it loses the rocket, the camera goes back to their last chasing view (or Auto)
//     with a toast. That is not saved: the next flight starts on the view they picked.
//  5. Drag and pinch orbit only from the open view. A touch that starts on any HUD panel (dock, notes, band, cards),
//     not just on a button, never orbits; drag distances are points, not device pixels, with a 10 pt slop.
using System;

namespace Kestrel.Core.Flight
{
    public enum FollowTapKind
    {
        /// <summary>The orbit is cleared on the same view.</summary>
        Recentre,
        /// <summary>The view changes (Pad to a chase, or a chase to Pad).</summary>
        SwitchView,
        /// <summary>Already following and Pad cannot see the rocket: nothing changes, a toast says so.</summary>
        AlreadyFollowing,
    }

    public struct FollowTap
    {
        public FollowTapKind Kind;
        /// <summary>The view after the tap.</summary>
        public FlightViewMode View;
        /// <summary>Clear the manual orbit (true for every tap that does not leave the view alone).</summary>
        public bool Recentre;
        /// <summary>Toast to show, or null.</summary>
        public string Toast;
    }

    public static class FlightCameraControl
    {
        /// <summary>Drag slop, points. A touch that moves less is a tap (it recentres).</summary>
        public const double SlopPt = 10;
        /// <summary>Orbit degrees per point dragged (yaw across, pitch up and down).</summary>
        public const double YawPerPt = 0.4, PitchPerPt = 0.3;

        public const string AlreadyFollowingToast = "Already following. Drag to look around.";
        public const string PadOutOfRangeToast = "Pad camera can't see the rocket now";

        /// <summary>A view the camera can use now. Pad needs the pad camera to resolve the rocket
        /// (FlightViewPose.PadInRange); every other view rides with the rocket and always can.</summary>
        public static bool Available(FlightViewMode mode, bool padInRange) => mode != FlightViewMode.Pad || padInRange;

        /// <summary>The view Follow returns to from Pad: the player's last chasing view, else Auto.</summary>
        public static FlightViewMode ChaseView(FlightViewMode lastChase) =>
            FlightView.Chases(lastChase) ? lastChase : FlightViewMode.Auto;

        /// <summary>What a tap on the Follow button does (rule 2).</summary>
        public static FollowTap Follow(FlightViewMode view, bool orbited, FlightViewMode lastChase, bool padInRange)
        {
            bool following = FlightView.Following(!orbited, view);
            if (!following && FlightView.Chases(view))
                return new FollowTap { Kind = FollowTapKind.Recentre, View = view, Recentre = true };
            if (!following)
            {
                var back = ChaseView(lastChase);
                return new FollowTap { Kind = FollowTapKind.SwitchView, View = back, Recentre = true, Toast = FlightView.Toast(back) };
            }
            if (padInRange)
                return new FollowTap { Kind = FollowTapKind.SwitchView, View = FlightViewMode.Pad, Recentre = true, Toast = FlightView.Toast(FlightViewMode.Pad) };
            return new FollowTap { Kind = FollowTapKind.AlreadyFollowing, View = view, Recentre = false, Toast = AlreadyFollowingToast };
        }

        /// <summary>The view the camera falls back to when the current one stops working (rule 4), or null to stay.</summary>
        public static FlightViewMode? Fallback(FlightViewMode view, FlightViewMode lastChase, bool padInRange) =>
            Available(view, padInRange) ? (FlightViewMode?)null : ChaseView(lastChase);

        /// <summary>The fallback toast: what happened and where the camera went.</summary>
        public static string FallbackToast(FlightViewMode to) => "Pad lost the rocket. " + FlightView.Toast(to);

        /// <summary>C on a keyboard: the next view in <see cref="FlightView.Cycle"/> the camera can use now.</summary>
        public static FlightViewMode Next(FlightViewMode view, bool padInRange)
        {
            var next = FlightView.Next(view);
            for (int i = 0; i < FlightView.Cycle.Length && !Available(next, padInRange); i++) next = FlightView.Next(next);
            return next;
        }

        /// <summary>The last chasing view after the player picks <paramref name="picked"/>.</summary>
        public static FlightViewMode RememberChase(FlightViewMode lastChase, FlightViewMode picked) =>
            FlightView.Chases(picked) ? picked : lastChase;

        /// <summary>Device pixels per UI point: the screen width over the panel's width in points
        /// (panel units / units-per-point). 1 when anything is unknown (the editor and device tests feed points).</summary>
        public static double PixelsPerPoint(double screenWidthPx, double panelWidthUnits, double unitsPerPoint)
        {
            if (!(screenWidthPx > 1) || !(panelWidthUnits > 1) || !(unitsPerPoint > 0)) return 1;
            double pts = panelWidthUnits / unitsPerPoint;
            double ppp = screenWidthPx / pts;
            return ppp > 0.25 && ppp < 8 ? ppp : 1;
        }

        /// <summary>
        /// The part of a drag (points from the touch-down) that orbits: zero inside the slop, then the distance past
        /// it along the same direction, so crossing the slop never jumps the view by the slop itself.
        /// </summary>
        public static void PastSlop(double dx, double dy, out double ox, out double oy)
        {
            double d = Math.Sqrt(dx * dx + dy * dy);
            if (!(d > SlopPt)) { ox = 0; oy = 0; return; }
            double k = (d - SlopPt) / d;
            ox = dx * k; oy = dy * k;
        }
    }
}
