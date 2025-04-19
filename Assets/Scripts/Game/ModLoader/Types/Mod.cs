namespace Game.ModLoader.Types
{
  
  public abstract class Mod
  {
    public abstract void OnLoad();
    public abstract void OnLoadComplete();
    
    public abstract void OnUnload();
  }
}