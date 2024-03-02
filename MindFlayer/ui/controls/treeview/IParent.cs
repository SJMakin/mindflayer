namespace MindFlayer;

interface IParent<T> where T : class, new()
{
    IEnumerable<T> GetChildren();
}