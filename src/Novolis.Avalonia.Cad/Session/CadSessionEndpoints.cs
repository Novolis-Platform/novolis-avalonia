namespace Novolis.Avalonia.Cad.Session;

public static class CadSessionMethodNames
{
    public const string Hello = "session.hello";
    public const string Snapshot = "session.snapshot";
    public const string Actions = "session.actions";
    public const string Command = "session.command";
    public const string Subscribe = "session.subscribe";

    public const string Changed = "session.changed";
    public const string ActionResult = "session.actionResult";
}

public static class CadSessionActionIds
{
    public const string New = "new";
    public const string Open = "open";
    public const string Save = "save";
    public const string Undo = "undo";
    public const string Redo = "redo";
    public const string DeleteSelection = "deleteselection";
    public const string Select = "select";
    public const string Fit = "fit";
    public const string SetTool = "settool";
    public const string SetViewMode = "setviewmode";
    public const string SetWorkspace = "setworkspace";
    public const string SetStudioWorkspace = "setstudioworkspace";
    public const string SetSelectionMode = "setselectionmode";
    public const string SetElevation = "setelevation";
    public const string SetSnap = "setsnap";
    public const string SetGrid = "setgrid";
    public const string SetAxisLock = "setaxislock";
    public const string RunCommand = "runcommand";
    public const string ExportPlanPng = "exportplanpng";
    public const string ExportModelPng = "exportmodelpng";
    public const string ExportPreviewPng = "exportpreviewpng";
    public const string ExportViewTour = "exportviewtour";
    public const string ExportPhys = "exportphys";

    public const string Boolean = "boolean";
    public const string Symmetry = "symmetry";
    public const string Clone = "clone";
    public const string Instance = "instance";
    public const string Connect = "connect";
    public const string Split = "split";
    public const string Group = "group";
    public const string MeshFromSolid = "meshfromsolid";
    public const string Weld = "weld";
    public const string Optimize = "optimize";
    public const string Bridge = "bridge";
    public const string AddMaterial = "addmaterial";
    public const string AddLight = "addlight";
    public const string AddCamera = "addcamera";

    public const string ExportScene = "exportscene";
    public const string BridgeScene = "bridgescene";
    public const string SetMaterial = "setmaterial";
    public const string SetWallSide = "setwallside";
    public const string AddWall = "addwall";
    public const string ExtrudeProfile = "extrudeprofile";
    public const string AddDimension = "adddimension";
    public const string AddLine = "addline";
    public const string AddCircle = "addcircle";
    public const string AddRect = "addrect";
    public const string AddSpline = "addspline";
    public const string AddBox = "addbox";
}

public static class CadSessionEndpoints
{
    public const string EnableEnvVar = "NOVOLIS_CAD_SESSION";
    public const string HttpEnableEnvVar = "NOVOLIS_CAD_SESSION_HTTP";
    public const string HttpPortEnvVar = "NOVOLIS_CAD_SESSION_HTTP_PORT";
    public const string TcpEnableEnvVar = "NOVOLIS_CAD_SESSION_TCP";
    public const string TcpPortEnvVar = "NOVOLIS_CAD_SESSION_TCP_PORT";
    public const string HttpMarkerFileName = "novolis-cad-session.http";
    public const string TcpMarkerFileName = "novolis-cad-session.tcp";
    public const int DefaultHttpPort = 18775;
    public const int DefaultTcpPort = 18776;

    public static bool IsEnabledByEnvironment() => EnvTruthy(EnableEnvVar);

    public static bool IsHttpEnabledByEnvironment()
    {
        var http = Environment.GetEnvironmentVariable(HttpEnableEnvVar);
        if (EnvFalsy(http))
            return false;
        if (EnvTruthyValue(http))
            return true;
        return IsEnabledByEnvironment();
    }

    public static bool IsTcpEnabledByEnvironment() => EnvTruthy(TcpEnableEnvVar);

    public static int ResolveHttpPort()
    {
        var raw = Environment.GetEnvironmentVariable(HttpPortEnvVar);
        return int.TryParse(raw, out var port) && port is > 0 and < 65536 ? port : DefaultHttpPort;
    }

    public static int ResolveTcpPort()
    {
        var raw = Environment.GetEnvironmentVariable(TcpPortEnvVar);
        return int.TryParse(raw, out var port) && port is > 0 and < 65536 ? port : DefaultTcpPort;
    }

    public static string HttpMarkerPath => Path.Combine(Path.GetTempPath(), HttpMarkerFileName);

    public static string TcpMarkerPath => Path.Combine(Path.GetTempPath(), TcpMarkerFileName);

    private static bool EnvTruthy(string name) => EnvTruthyValue(Environment.GetEnvironmentVariable(name));

    private static bool EnvTruthyValue(string? value) =>
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private static bool EnvFalsy(string? value) =>
        string.Equals(value, "0", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "no", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);
}
