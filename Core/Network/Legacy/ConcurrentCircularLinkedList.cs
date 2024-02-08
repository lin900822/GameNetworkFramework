namespace Core.Network.Legacy;

public class Node<T>
{
    public T       Data;
    public Node<T> NextNode;

    public Node(T data)
    {
        Data = data;
    }
}

public class ConcurrentCircularLinkedList<T>
{
    public int Count { get; private set; }
    
    private Node<T> _head;
    private Node<T> _tail;

    private SpinLock _spinLock;

    public ConcurrentCircularLinkedList()
    {
        _spinLock = new SpinLock();
    }

    public void Push(T data)
    {
        var node = new Node<T>(data);

        var lockToken = false;
        _spinLock.Enter(ref lockToken);
        if (_head == null)
        {
            _head         = node;
            _tail         = node;
            node.NextNode = node;
            ++Count;
            
            if(lockToken) _spinLock.Exit();
            return;
        }

        _tail.NextNode = node;
        _tail          = node;
        _tail.NextNode = _head;
            
        ++Count;
            
        if(lockToken) _spinLock.Exit();
    }

    public void Pop()
    {
        
    }
}