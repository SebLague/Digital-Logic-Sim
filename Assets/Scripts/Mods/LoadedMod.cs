using UnityEngine;
using System.Reflection;

public class LoadedMod
{
    readonly string name;
    readonly string version;
    readonly object instance;
    readonly MethodInfo initializeMethod;

    public LoadedMod(string name, string version, object instance, MethodInfo initMethod)
    {
        this.name = name;
        this.version = version;
        this.instance = instance;
        initializeMethod = initMethod;
    }

    public void Initialize()
    {
        try
        {
            initializeMethod.Invoke(instance, null);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Mod {name} failed to initialize:\n{e}");
        }
    }
}