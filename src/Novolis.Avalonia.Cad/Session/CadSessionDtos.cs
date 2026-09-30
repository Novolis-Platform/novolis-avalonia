namespace Novolis.Avalonia.Cad.Session;

public interface ICadSession
{
    CadHelloResponseDto Hello();

    CadSnapshotDto Snapshot();

    CadActionsResponseDto Actions();

    CadCommandResultDto Execute(CadCommandDto command);

    void Subscribe();

    event Action<CadChangedEventDto>? Changed;

    event Action<CadActionResultEventDto>? ActionResult;
}
