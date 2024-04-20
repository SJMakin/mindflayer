namespace MindFlayer.saas;

public abstract class ChatProvider
{
    public abstract Task<string> Chat(IEnumerable<ChatMessage> messages, double? temp, string model);
    public abstract Task ChatStream(IEnumerable<ChatMessage> messages, double? temp, Action<string> callback, string model);
}
