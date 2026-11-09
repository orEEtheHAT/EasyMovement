namespace EasyMovement.FirstPerson
{
    public interface ICharacterMovementStateProvider
    {
        bool IsSprinting();
        bool IsCrouched();
    }
}
