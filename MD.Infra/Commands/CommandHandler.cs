namespace MD.Infra.Commands;

internal class CommandHandler
{
    private readonly List<Command> _commands;

    internal CommandHandler(List<Command> commands)
    {
        _commands = commands;
    }

    internal async Task<ExecutionResult> Execute(string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var inputCommand = parts[0];
        var arguments = parts.Length <= 1
                            ? null
                            : string.Join(' ', parts.Skip(1));

        if (!Enum.TryParse<CommandType>(inputCommand, true, out var commandType))
            return ExecutionResult.FailValidation($"Unknown command: '{inputCommand}'");

        var command = _commands.FirstOrDefault(c => c.Type == commandType);

        if (command is null)
            return ExecutionResult.FailValidation($"Command not found: '{inputCommand}'");

        return await command.Execute(arguments);
    }

}