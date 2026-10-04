namespace Foldspace.Rendering
{
    /// <summary>
    /// Sorting orders for every renderer in the game, back to front. Everything sits on the Default sorting layer.
    /// </summary>
    public static class SortOrder
    {
        public const int Nebula = -100;
        public const int FarObjects = -90;
        public const int FarStars = -80;
        public const int NearStars = -70;
        public const int Lens = -30;
        public const int LensShade = -29;
        public const int Grid = -28;
        public const int RimGlow = -22;
        public const int Rim = -21;
        public const int RimDetail = -20;
        public const int Xp = -18;
        public const int Shield = -8;
        public const int FoldFill = -5;
        public const int WakeGlow = -2;
        public const int Wake = 0;
        public const int Glow = 3;
        public const int Pickups = 5;
        public const int Enemies = 10;
        public const int Bullets = 12;
        public const int Thrust = 14;
        public const int Player = 15;
        public const int Fx = 20;
        public const int Telegraph = 25;
    }
}
