namespace MindFlayer;

internal class History
{
    public History(Operation operation, string input, string output)
    {
        Operation = operation;
        Input = input;
        Output = output;
    }

    Operation Operation { get; set; }
    string Input { get; set; }
    string Output { get; set; }
}
