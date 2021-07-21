namespace Pecan
{
    internal class VoidHolder
    {
        public static object AsObject { get; } = new VoidHolder();

        private VoidHolder()
        {
        }
    }
}
