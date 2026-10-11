using Unshackle.Interface;

// None is fine: System.CommandLine links Ctrl+C to the token it passes to each command action.
return await Cli.RunAsync(args, CancellationToken.None);
