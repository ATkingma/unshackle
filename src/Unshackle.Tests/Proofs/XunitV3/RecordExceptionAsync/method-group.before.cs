using System;
using System.Threading.Tasks;
using Xunit;

public static class Checks
{
    public static readonly Func<Func<Task>, Task<Exception>> Record_ = Record.ExceptionAsync;
}
