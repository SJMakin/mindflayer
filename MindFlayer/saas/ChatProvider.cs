namespace MindFlayer.saas;

public abstract class ChatProvider
{
    public abstract Task<string> Chat(ChatContext chatl);
    public abstract Task ChatStream(ChatContext chat);
}
