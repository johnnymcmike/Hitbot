namespace Hitbot;

public abstract class IBookKeeper
{
    public abstract void BookSet(string key, int num);
    public abstract int BookGet(string key);
    public abstract void BookIncr(string key, int by = 1);
    public abstract void BookDecr(string key, int by = 1);
    public abstract bool BookHasKey(string key);
    public abstract void BookClear();
}