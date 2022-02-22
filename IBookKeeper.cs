namespace Hitbot;

public interface IBookKeeper
{
    void BookSet(string key, int num);
    int BookGet(string key);
    void BookIncr(string key, int by = 1);
    void BookDecr(string key, int by = 1);
    bool BookHasKey(string key);
    void BookClear();
}