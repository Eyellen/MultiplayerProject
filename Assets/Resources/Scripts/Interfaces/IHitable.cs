namespace GameEngine.Core
{
    public interface IHitable
    {
        /// <returns>True if hit was successful, False otherwise.</returns>
        public bool Hit();
    }
}
