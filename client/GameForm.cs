using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SigmaGame;

sealed class GameForm : Form
{
    readonly TextBox server = new() { Text = "wss://sigmachat-server.onrender.com/ws", Width = 330 };
    readonly TextBox room = new() { Text = "SIGMA-PRIVATE", Width = 180 };
    readonly TextBox playerName = new() { Text = $"User{Random.Shared.Next(100,999)}", Width = 180 };
    readonly Button connect = new() { Text = "JOIN PRIVATE ROOM", Width = 180, Height = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(63, 89, 255), ForeColor = Color.White };
    readonly Label status = new() { AutoSize = true, ForeColor = Color.Silver };
    readonly Panel login = new() { BackColor = Color.FromArgb(28, 31, 45), Width = 440, Height = 380 };
    readonly FlowLayoutPanel messages = new() { AutoScroll = true, WrapContents = false, FlowDirection = FlowDirection.TopDown, BackColor = Color.FromArgb(18,20,30) };
    readonly ListBox members = new() { BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(28,31,45), ForeColor = Color.White, Font = new Font("Segoe UI", 10) };
    readonly TextBox compose = new() { Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(37,41,57), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical };
    readonly Button send = new() { Text = "SEND", BackColor = Color.FromArgb(63,89,255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    readonly Button attach = new() { Text = "IMAGE", BackColor = Color.FromArgb(48,52,70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    readonly Button attachFile = new() { Text = "FILE", BackColor = Color.FromArgb(48,52,70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
    readonly Label header = new() { ForeColor = Color.White, Font = new Font("Segoe UI", 13, FontStyle.Bold), Text = "SigmaChat" };
    ClientWebSocket? socket; CancellationTokenSource? cts; string myId = "", activeRoom = "";
    readonly List<SavedItem> history = [];
    readonly string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SigmaChat");

    public GameForm()
    {
        Text="SigmaChat — Private Rooms"; ClientSize=new(920,600); MinimumSize=new(720,480); BackColor=Color.FromArgb(18,20,30); KeyPreview=true;
        Controls.AddRange([header,messages,members,compose,attach,attachFile,send,login]); Directory.CreateDirectory(dataFolder);
        var title=new Label { Text="Σ  SIGMACHAT", Font=new Font("Segoe UI",24,FontStyle.Bold), ForeColor=Color.DeepSkyBlue, AutoSize=true, Left=72,Top=25 }; login.Controls.Add(title);
        AddField("Server",server,105); AddField("Private room code",room,165); AddField("Your name",playerName,225);
        connect.Left=130; connect.Top=280; login.Controls.Add(connect); status.Left=20; status.Top=335; login.Controls.Add(status);
        connect.Click += async (_,_) => await Join(); send.Click += async (_,_) => await SendChat(); attach.Click += async (_,_) => await SendImage(); attachFile.Click += async(_,_)=>await SendFile(); Shown += async(_,_)=>await Updater.Check(this);
        compose.KeyDown += async (_,e) => { if(e.KeyCode==Keys.Enter && !e.Shift) { e.SuppressKeyPress=true; await SendChat(); } };
        Resize += (_,_) => LayoutUi(); FormClosing += (_,_) => cts?.Cancel(); LayoutUi(); ShowLogin(true);
    }
    void AddField(string label,Control field,int top) { login.Controls.Add(new Label{Text=label,ForeColor=Color.White,Left=35,Top=top+5,Width=115});field.Left=150;field.Top=top;login.Controls.Add(field); }
    void LayoutUi()
    {
        login.Left=(ClientSize.Width-login.Width)/2;login.Top=(ClientSize.Height-login.Height)/2;
        header.SetBounds(18,12,ClientSize.Width-36,32); members.SetBounds(ClientSize.Width-190,55,175,ClientSize.Height-120);
        messages.SetBounds(18,55,ClientSize.Width-225,ClientSize.Height-155); compose.SetBounds(18,ClientSize.Height-87,ClientSize.Width-495,69);attachFile.SetBounds(ClientSize.Width-465,ClientSize.Height-69,80,34);attach.SetBounds(ClientSize.Width-375,ClientSize.Height-69,80,34); send.SetBounds(ClientSize.Width-285,ClientSize.Height-69,90,34);
    }
    void ShowLogin(bool show) { login.Visible=show; header.Visible=messages.Visible=members.Visible=compose.Visible=attach.Visible=attachFile.Visible=send.Visible=!show; }
    async Task Join()
    {
        connect.Enabled=false;status.Text="Connecting…";
        try { socket=new ClientWebSocket();cts=new();activeRoom=SafeName(room.Text.ToUpperInvariant());await socket.ConnectAsync(new Uri(server.Text.Trim()),cts.Token);await SendJson(new{type="join",room=room.Text,name=playerName.Text});LoadHistory();ShowLogin(false);_ = ReceiveLoop(cts.Token); }
        catch(Exception ex){status.Text="Could not connect: "+ex.Message;connect.Enabled=true;}
    }
    async Task SendChat()
    {
        var text=compose.Text.Trim();if(text.Length==0||socket?.State!=WebSocketState.Open)return;compose.Clear();
        try{await SendJson(new{type="chat",message=text});}catch{Disconnect("Connection lost.");}
    }
    async Task SendImage()
    {
        if(socket?.State!=WebSocketState.Open)return;
        using var picker=new OpenFileDialog{Title="Choose an image",Filter="Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp"};if(picker.ShowDialog()!=DialogResult.OK)return;
        try
        {
            using var original=Image.FromFile(picker.FileName);var scale=Math.Min(1.0,1200.0/Math.Max(original.Width,original.Height));
            using var resized=new Bitmap((int)(original.Width*scale),(int)(original.Height*scale));using(var g=Graphics.FromImage(resized))g.DrawImage(original,0,0,resized.Width,resized.Height);
            using var ms=new MemoryStream();resized.Save(ms,System.Drawing.Imaging.ImageFormat.Jpeg);
            if(ms.Length>1_000_000){MessageBox.Show("Please choose a smaller image.","Image too large");return;}
            await SendJson(new{type="image",image=Convert.ToBase64String(ms.ToArray())});
        } catch(Exception ex){MessageBox.Show("Could not send image: "+ex.Message);}
    }
    async Task SendFile()
    {
        if(socket?.State!=WebSocketState.Open)return;using var picker=new OpenFileDialog{Title="Choose a file (maximum 5 MB)",Filter="All files|*.*"};if(picker.ShowDialog()!=DialogResult.OK)return;
        try{var info=new FileInfo(picker.FileName);if(info.Length>5_000_000){MessageBox.Show("Files must be 5 MB or smaller.");return;}var bytes=await File.ReadAllBytesAsync(picker.FileName);await SendJson(new{type="file",fileName=info.Name,data=Convert.ToBase64String(bytes)});}catch(Exception ex){MessageBox.Show("Could not send file: "+ex.Message);}
    }
    async Task SendJson(object value){var data=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));await socket!.SendAsync(data,WebSocketMessageType.Text,true,cts!.Token);}
    async Task ReceiveLoop(CancellationToken ct)
    {
        var buffer=new byte[16384];
        try
        {
            while(socket?.State==WebSocketState.Open)
            {
                using var payload=new MemoryStream();WebSocketReceiveResult r;
                do { r=await socket.ReceiveAsync(buffer,ct);if(r.MessageType==WebSocketMessageType.Close)return;payload.Write(buffer,0,r.Count);if(payload.Length>9_000_000)throw new InvalidDataException("Message too large"); } while(!r.EndOfMessage);
                using var doc=JsonDocument.Parse(payload.ToArray());var root=doc.RootElement;var type=root.GetProperty("type").GetString();
                if(type=="keyRequired")
                {
                    var creating=root.GetProperty("create").GetBoolean();var key=(string?)Invoke(()=>KeyPrompt.Ask(this,creating));
                    if(string.IsNullOrEmpty(key)){BeginInvoke(()=>Disconnect("A room key is required."));return;}await SendJson(new{type="auth",key});
                }
                else if(type=="welcome")
                {
                    myId=root.GetProperty("id").GetString()??"";
                    var joinedRoom=root.GetProperty("room").GetString()??"PRIVATE ROOM";
                    BeginInvoke(()=>header.Text=$"🔒  {joinedRoom}   •   SigmaChat");
                }
                else if(type=="members") { var names=root.GetProperty("members").EnumerateArray().Select(x=>"●  "+x.GetString()).ToArray();BeginInvoke(()=>{members.Items.Clear();members.Items.Add("ROOM MEMBERS");members.Items.AddRange(names);}); }
                else if(type=="notice") Append("SYSTEM",root.GetProperty("message").GetString()??"",Color.Gray,null,false,true);
                else if(type=="chat") { var senderId=root.GetProperty("senderId").GetString();Append(root.GetProperty("sender").GetString()??"",root.GetProperty("message").GetString()??"",senderId==myId?Color.DeepSkyBlue:Color.MediumPurple,root.GetProperty("id").GetString(),senderId==myId); }
                else if(type=="image") { var senderId=root.GetProperty("senderId").GetString();SaveAndAppendImage(root.GetProperty("sender").GetString()??"",root.GetProperty("image").GetString()??"",root.GetProperty("id").GetString(),senderId==myId); }
                else if(type=="file") { var senderId=root.GetProperty("senderId").GetString();SaveAndAppendFile(root.GetProperty("sender").GetString()??"",root.GetProperty("fileName").GetString()??"file",root.GetProperty("data").GetString()??"",root.GetProperty("id").GetString(),senderId==myId); }
                else if(type=="delete") { var id=root.GetProperty("id").GetString();if(id is not null)BeginInvoke(()=>DeleteLocal(id)); }
                else if(type=="error") { Disconnect(root.GetProperty("message").GetString()??"Server error");return; }
            }
        } catch { }
        if(!ct.IsCancellationRequested) BeginInvoke(()=>Disconnect("Disconnected from server."));
    }
    void Append(string name,string text,Color color,string? id=null,bool mine=false,bool system=false,bool save=true) => BeginInvoke(()=>
    {
        var label=new Label{AutoSize=false,Width=Math.Max(300,messages.ClientSize.Width-35),Height=system?30:52,Padding=new Padding(10,system?5:7,10,4),Margin=new Padding(0,2,0,2),BackColor=system?Color.FromArgb(22,24,34):Color.FromArgb(29,32,46),ForeColor=system?Color.DarkGray:Color.Gainsboro,Text=system?$"• {text}":$"{name}   {DateTime.Now:t}\r\n{text}",Tag=id};
        label.Font=new Font("Segoe UI",system?8.5f:10,system?FontStyle.Italic:FontStyle.Regular);if(id is not null)AddDeleteMenu(label,id,mine);messages.Controls.Add(label);messages.ScrollControlIntoView(label);
        if(save&&id is not null){history.Add(new SavedItem(id,"text",name,text,null,DateTime.Now,mine));SaveHistory();}
    });
    void SaveAndAppendImage(string name,string base64,string? id,bool mine)
    {
        try { var path=Path.Combine(dataFolder,$"image-{Guid.NewGuid():N}.jpg");File.WriteAllBytes(path,Convert.FromBase64String(base64));BeginInvoke(()=>AddImage(name,path,id,mine,true)); } catch { Append("SYSTEM","An image could not be displayed.",Color.Gray,null,false,true); }
    }
    void SaveAndAppendFile(string sender,string fileName,string base64,string? id,bool mine)
    {
        try{var folder=Path.Combine(dataFolder,"Received Files");Directory.CreateDirectory(folder);var safe=Path.GetFileName(fileName);var path=Path.Combine(folder,$"{Guid.NewGuid():N}-{safe}");File.WriteAllBytes(path,Convert.FromBase64String(base64));BeginInvoke(()=>AddFile(sender,safe,path,id,mine,true));}catch{Append("SYSTEM","A file could not be received.",Color.Gray,null,false,true);}
    }
    void AddFile(string sender,string fileName,string path,string? id,bool mine,bool save)
    {
        var panel=new Panel{Width=Math.Max(300,messages.ClientSize.Width-35),Height=76,Margin=new Padding(0,2,0,2),BackColor=Color.FromArgb(29,32,46),Tag=id};panel.Controls.Add(new Label{Text=$"{sender} sent: {fileName}",ForeColor=Color.Gainsboro,Left=10,Top=8,Width=panel.Width-130});var saveButton=new Button{Text="Save As…",Left=panel.Width-110,Top=18,Width=95,Height=34};saveButton.Click+=(_,_)=>{using var dialog=new SaveFileDialog{FileName=fileName};if(dialog.ShowDialog()==DialogResult.OK)File.Copy(path,dialog.FileName,true);};panel.Controls.Add(saveButton);if(id is not null)AddDeleteMenu(panel,id,mine);messages.Controls.Add(panel);if(save&&id is not null){history.Add(new SavedItem(id,"file",sender,fileName,path,DateTime.Now,mine));SaveHistory();}
    }
    void AddImage(string name,string path,string? id,bool mine,bool save)
    {
        var panel=new Panel{Width=Math.Max(300,messages.ClientSize.Width-35),Height=230,Margin=new Padding(0,2,0,2),BackColor=Color.FromArgb(29,32,46),Tag=id};
        panel.Controls.Add(new Label{Text=$"{name}   {DateTime.Now:t}",ForeColor=Color.DeepSkyBlue,Left=9,Top=6,Width=panel.Width-18});
        try{panel.Controls.Add(new PictureBox{Image=Image.FromFile(path),SizeMode=PictureBoxSizeMode.Zoom,Left=8,Top=29,Width=panel.Width-16,Height=192});}catch{return;}
        if(id is not null)AddDeleteMenu(panel,id,mine);messages.Controls.Add(panel);messages.ScrollControlIntoView(panel);if(save&&id is not null){history.Add(new SavedItem(id,"image",name,null,path,DateTime.Now,mine));SaveHistory();}
    }
    void AddDeleteMenu(Control control,string id,bool mine)
    {
        var menu=new ContextMenuStrip();menu.Items.Add("Delete for me",null,(_,_)=>DeleteLocal(id));
        if(mine)menu.Items.Add("Delete for everyone",null,async(_,_)=>{try{await SendJson(new{type="delete",id});}catch{}});
        control.ContextMenuStrip=menu;foreach(Control child in control.Controls)child.ContextMenuStrip=menu;
    }
    void DeleteLocal(string id)
    {
        var control=messages.Controls.Cast<Control>().FirstOrDefault(x=>x.Tag as string==id);if(control is not null){messages.Controls.Remove(control);control.Dispose();}
        history.RemoveAll(x=>x.Id==id);SaveHistory();
    }
    void LoadHistory()
    {
        messages.Controls.Clear();history.Clear();var path=HistoryPath();
        try{history.AddRange(JsonSerializer.Deserialize<List<SavedItem>>(File.ReadAllText(path))??[]);}catch{}
        foreach(var item in history.ToArray())if(item.Type=="image"&&item.Path is not null&&File.Exists(item.Path))AddImage(item.Sender,item.Path,item.Id,item.Mine,false);else if(item.Type=="file"&&item.Path is not null&&File.Exists(item.Path))AddFile(item.Sender,item.Text??Path.GetFileName(item.Path),item.Path,item.Id,item.Mine,false);else if(item.Text is not null)Append(item.Sender,item.Text,Color.Gainsboro,item.Id,item.Mine,false,false);
    }
    void SaveHistory(){try{File.WriteAllText(HistoryPath(),JsonSerializer.Serialize(history));}catch{}}
    string HistoryPath()=>Path.Combine(dataFolder,$"history-{activeRoom}.json");
    static string SafeName(string value)=>new(value.Where(char.IsLetterOrDigit).Take(24).ToArray());
    void Disconnect(string reason){cts?.Cancel();socket?.Dispose();socket=null;status.Text=reason;connect.Enabled=true;ShowLogin(true);}
}
sealed record SavedItem(string Id,string Type,string Sender,string? Text,string? Path,DateTime Time,bool Mine);
