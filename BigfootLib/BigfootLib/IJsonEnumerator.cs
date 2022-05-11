namespace BigfootLib
{
    public interface IJsonEnumerator
    {
        Span<byte> GetNextJsonFragment();
    }
}
