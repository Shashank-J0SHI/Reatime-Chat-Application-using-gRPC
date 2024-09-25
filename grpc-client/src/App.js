import zero from './assets/Avatar/0.png'
import one from './assets/Avatar/1.png'
import two from './assets/Avatar/2.png'
import three from './assets/Avatar/3.png'

import './App.css';

import { Player } from '@lottiefiles/react-lottie-player';
import hello from "./assets/images/hello.json"

import ChatMsg from './Components/ChatMessage'

import { ChatMessage, Empty, User, ReceiveMsgRequest } from "./chat_pb";
import { ChatServiceClient } from "./chat_grpc_web_pb";
import { useState, useRef, useEffect } from "react";
import { createRoot } from 'react-dom/client';

const client = new ChatServiceClient("http://192.168.169.88:8080", null, null);
const profileList = [zero, one, two, three];

let _token = "";

function App() {

  const [_user, setUser] = useState("")
  const [joinError, setJoin] = useState()
  const [UserL, setList] = useState([])
  const [profile, setProfile] = useState(0)
  const [role, setRole] = useState()

  const msgfield = useRef(0);
  const userfield = useRef(0);
  const redError = useRef(0);
  const overlay = useRef(0);
  const sidebar = useRef(0);

  useEffect(() => {
    if (_user !== "")
    {
      let rmr = new ReceiveMsgRequest();
      rmr.setUser(_user);
      rmr.setToken("123456");

      const metadata = {
        "Authorization": _token
      }
      
      let chatStream = client.receiveMsg(rmr, metadata);
      chatStream.on("data", (response) => {
        let created = document.createElement('div');
        created.setAttribute('id', "msg");
        
        let bottom = document.getElementById('bottom');
        bottom.parentNode.insertBefore(created, bottom);
  
        let root = createRoot(created);
        if (response.getFrom() === _user)
        {
          root.render(<ChatMsg nm={response.getFrom()} msg={response.getMsg()} time={response.getTime()}/>)
        }
        else
        {
          root.render(<ChatMsg type={false} nm={response.getFrom()} msg={response.getMsg()} time={response.getTime()}/>)
        }
  
        setTimeout(() => {
          console.log(hello)
          bottom.scrollIntoView();
        }, 1)
      });

      let UserListStream = client.getAllUsers(new Empty(), metadata);
      UserListStream.on("data", (response) => {
        setList(response.getUsersList());
      });
    }
  }, [_user])

  function clickhndl()
  {
    if (msgfield.current.value !== "")
    {
      let msg = new ChatMessage();
      msg.setMsg(msgfield.current.value);
      msg.setFrom(_user);
      msg.setTime(new Date().toLocaleString());

      const metadata = {
        "Authorization": _token
      }

      client.sendMsg(msg, metadata, (err) => {
        if (err) return console.log(err);
      });

      msgfield.current.value = "";
    }
  }

  async function joiner(id)
  {
    let user = new User();
    setRole(id);
    user.setId(id);
    user.setName(userfield.current.value);
    user.setProfile(profile);

    await client.join(user, null, (err, response) => {
      if (err) {
        alert("Unable to connect to server.");
        return console.log(err);
      }
      
      if (response.getError() === 0)
      {
        _token = response.getMsg();
        setUser(user.getName());
        redError.current.style.visibility = "hidden";
        overlay.current.classList.add('active');
        overlay.current.style.pointerEvents = "none";
      }
      else
      {
        setJoin(response.getMsg())
        redError.current.style.visibility = "visible";
        overlay.current.style.pointerEvents = "all";
      }
    });
  }

  function show()
  {
    sidebar.current.classList.toggle('active');
  }

  function select(num)
  {
    return profileList[num];
  }

  return (
    <div className="App">
      <header className="App-header">
        <h1>Chat App</h1>
        <div id="detail">
          <div id="ppic" style={{backgroundImage: `url(${select(profile)})`}} onClick={show}></div>
          <p>Logged in as <span>{_user}</span></p>
        </div>
      </header>

      <div className="Sidebar" ref={sidebar}>
        {UserL.map((users, index) => (
          <div key={index} id="others">
            <div id="divImg" style={{backgroundImage: `url(${select(users.getProfile())})`}}></div>
            <div id="divInfo">
              <h4 style={{color: "white", fontSize: "18px", fontWeight: "500"}}>{users.getName()}</h4>
              <p>{users.getId()}</p>
            </div>
          </div>
        ))}
      </div>

      <div className='chatSpace'>
        <Player
          id="lot"
          src={hello}
          className="player"
          loop
          autoplay
        />

        <div id="bottom"></div>
        {(role === "Participant") ? <div id="msgfield"><input className="txtfield" ref={msgfield} style={{position: "relative", backgroundColor: "rgb(15, 15, 18)", height: "55px", width: "90%", left: "0", color: "white", fontSize: "18px", padding: "0px 15px", outline: "none", border: "none"}} placeholder="Write your message here."></input><div style={{position: "relative", backgroundColor: "rgb(15, 15, 18)", height: "100%", width: "60px", borderLeft: "1px white solid"}} onClick={clickhndl}></div></div> : null}
      </div>

      <div id='overlay' ref={overlay}>
        <div id="selectedP" style={{backgroundImage: `url(${select(profile)})`}}></div>
        <div id="profile">
          <div id="pimg" tabIndex="-1" onClick={() => {setProfile(0)}}></div>
          <div id="pimg" tabIndex="-1" onClick={() => {setProfile(1)}}></div>
          <div id="pimg" tabIndex="-1" onClick={() => {setProfile(2)}}></div>
          <div id="pimg" tabIndex="-1" onClick={() => {setProfile(3)}}></div>
        </div>
        <h2>Welcome.</h2>
        <p>Continue with your Name.</p>
        <input className="userfield" ref={userfield} placeholder="Write your name."></input>
        <p id="redError" style={{color: "red", fontSize: "18px"}} ref={redError}>{joinError}</p>
        <p>Continue as.</p>
        <div id="selector">
          <div className="option" onClick={() => {joiner("Observer")}}>
            <div className='optImg' id="one"></div>
            <p>Observer</p>
          </div>
          <div style={{position: "relative", height: "100%", width: "fit-content", display: "flex", alignItems: "center", justifyContent: "center", margin: "0 40px"}}>
            <div style={{position: "relative", height: "80%", borderLeft: "1px white solid"}}></div>
            <p style={{position: "absolute", transform: "translate(-50%, -50%)", left: "50%", top: "50%", backgroundColor: "rgb(15, 15, 18)", padding: "5px 0", margin: "0 0"}}>or</p>
          </div>
          <div className="option" onClick={() => {joiner("Participant")}}>
            <div className='optImg' id="two"></div>
            <p>Participant</p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default App;
