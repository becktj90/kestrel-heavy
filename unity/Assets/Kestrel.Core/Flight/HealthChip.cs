// How a vehicle-health tab (Eng, Tnk, Hull, Guide, Fair) draws. The chip's colour is the part's real health; a tab
// being open is shown separately, by a cream ring (same width either way), and never changes the health colours. Before (#332 follow-up):
// every chip was dusk ink with a tone-coloured label, and the open tab swapped to the dark alarm plate, so a healthy
// Eng or Hull looked red only because it was open. Static colours, no blink or pulse (photosensitivity-safe).
// No Unity types; AscentHud converts.
namespace Kestrel.Core.Flight
{
    public struct HealthChipLook
    {
        public CountdownStack.Rgba Plate, Text, Edge, Ring;
        public float EdgePt, RingPt;
    }

    public static class HealthChip
    {
        public const float EdgePt = 2f;
        public const float RingPt = 2f;

        public static readonly CountdownStack.Rgba GreenPlate = CountdownStack.Rgba.Bytes(10, 34, 30, 0.94f);
        public static readonly CountdownStack.Rgba GreenText = CountdownStack.Rgba.Bytes(96, 226, 150);
        public static readonly CountdownStack.Rgba AmberPlate = CountdownStack.Rgba.Bytes(50, 34, 8, 0.94f);
        public static readonly CountdownStack.Rgba AmberText = CountdownStack.Rgba.Bytes(255, 190, 90);
        public static readonly CountdownStack.Rgba RedPlate = CountdownStack.Rgba.Bytes(64, 12, 16, 0.94f);
        public static readonly CountdownStack.Rgba RedText = CountdownStack.Rgba.Bytes(255, 138, 120);
        /// <summary>The open tab's ring: cream, the dusk HUD's neutral, not a health colour.</summary>
        public static readonly CountdownStack.Rgba OpenRing = CountdownStack.Rgba.Bytes(246, 235, 210);

        public static HealthChipLook Look(VehicleHealthTone tone, bool open)
        {
            CountdownStack.Rgba plate, text;
            switch (tone)
            {
                case VehicleHealthTone.Red: plate = RedPlate; text = RedText; break;
                case VehicleHealthTone.Amber: plate = AmberPlate; text = AmberText; break;
                default: plate = GreenPlate; text = GreenText; break;
            }
            return new HealthChipLook
            {
                Plate = plate,
                Text = text,
                Edge = text,
                EdgePt = EdgePt,
                // The ring is always RingPt wide, so opening a tab never moves or narrows its label; closed, it is the plate.
                Ring = open ? OpenRing : plate,
                RingPt = RingPt,
            };
        }

        /// <summary>Red-dominant colour (the alarm family): red well above green and blue.</summary>
        public static bool Reddish(CountdownStack.Rgba c) => c.R > c.G * 1.6f && c.R > c.B * 1.6f && c.R > 0.12f;
    }
}
