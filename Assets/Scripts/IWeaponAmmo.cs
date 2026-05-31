// Shared ammo readout so UI can read any weapon implementation
// (global Weapon used by Level4, Sponge.Weapon used by other levels).
public interface IWeaponAmmo
{
    int CurrentMag { get; }
    int CurrentReserve { get; }
    int MaxMag { get; }
}
