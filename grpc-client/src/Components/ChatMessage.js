import './ChatMessage.css' 

export default function ChatMsg({type = true, nm = "User", msg = "hello", time = "unknown"})
{
    if (type)
    {
        return (
            <div className="div" id="g">
                <p style={{color: "white", fontSize: "22px", fontWeight: "500", marginBottom: "8px", textAlign: "left"}}>{msg}</p>
                <p style={{color: "white", fontSize: "16px", fontWeight: "300", marginTop: "0", textAlign: "right"}}>{time}</p>
            </div>
        )
    }
    else
    {
        return (
            <div className="div" id="f">
                <p style={{color: "white", fontSize: "18px", fontWeight: "300", marginTop: "0", marginBottom: "5px", textAlign: "left"}}>{nm}</p>
                <p style={{color: "white", fontSize: "22px", fontWeight: "500", marginTop: "0", marginBottom: "8px", textAlign: "left"}}>{msg}</p>
                <p style={{color: "white", fontSize: "16px", fontWeight: "300", marginTop: "0", marginBottom: "10px", textAlign: "left"}}>{time}</p>
            </div>
        );
    }
}