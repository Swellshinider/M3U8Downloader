using System.Threading.Tasks;
using MD.Infra;

namespace MD;

internal static partial class Program
{
    public static async Task<int> Main()
    {
        try
        {
            await Application.Run();
            return 0;
        }
        catch (Exception ex)
        {
            $"Fatal Error: {ex.Message}\n".WriteLine(ConsoleColor.Red);
            return -1;
        }
    }
}