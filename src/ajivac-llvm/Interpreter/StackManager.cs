namespace ajivac_llvm;

public class StackManager
{
    public StackManager(Action<string> logger)
    {
        this.logger = logger;
    }
    
    private readonly Stack<InternStackFrame> stack = new();
    private readonly Action<string> logger;

    public class InternStackFrame
    {
        public Dictionary<string, object?> Variables { get; set; } = new();
    }

    public object? GetVariable(string name)
    {
        int index = 0;
        foreach (var frame in stack)
        {
            index++;
            if (!frame.Variables.ContainsKey(name))
                continue;
            logger($"get variable {name} with {frame.Variables[name]} on stack [{index}]");
            return frame.Variables[name];
        }
        throw new Exception($"Variable {name} not found");
    }

    public void CreateVariable(string name, object value)
    {
        var frame = stack.Peek();
        logger($"set variable {name} to {value}");
        frame.Variables[name] = value;
    }

    public void SetVariable(string name, object? value)
    {
        int index = 0;
        foreach (var frame in stack)
        {
            index++;
            if (!frame.Variables.ContainsKey(name))
                continue;
            logger($"updated variable {name} from {frame.Variables[name]} to {value} on stack [{index}]");
            frame.Variables[name] = value;
            return;
        }

        throw new Exception($"Variable {name} not found");
    }

    public InternStackFrame Push(InternStackFrame? stackFrame = null)
    {
        logger("new stack frame");
        stackFrame ??= new InternStackFrame();
        stack.Push(stackFrame);
        return stackFrame;
    }

    public InternStackFrame Pop()
    {
        logger("pop stack frame");
        return stack.Pop();
    }
}
