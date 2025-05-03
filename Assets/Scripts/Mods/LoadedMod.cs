using System.Reflection;
using DLS.Game;

public class LoadedMod
{
    readonly string name;
    readonly string version;
    readonly object instance;
    readonly MethodInfo initializeMethod;
    readonly MethodInfo onProjectLoadMethod;

    public LoadedMod(string name, string version, object instance, MethodInfo initMethod, MethodInfo onProjectMethod)
    {
        this.name = name;
        this.version = version;
        this.instance = instance;
        initializeMethod = initMethod;
        onProjectLoadMethod = onProjectMethod;
    }

    public void Initialize()
    {
        try
        {
            initializeMethod.Invoke(instance, null);
        }
        catch (System.Exception e)
        {
            throw new System.Exception($"Mod {name} failed to initialize:\n{e}");
        }
    }

    public void OnProjectLoad()
    {
        try
        {
            onProjectLoadMethod.Invoke(instance, null);
        }
        catch (System.Exception e)
        {
            throw new System.Exception($"Mod {name} failed on project load:\n{e}");
        }
    }
}