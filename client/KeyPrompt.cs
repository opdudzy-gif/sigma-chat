namespace SigmaGame;
static class KeyPrompt
{
    public static string? Ask(Form owner,bool creating)
    {
        using var form=new Form{Text=creating?"Create room key":"Room key required",ClientSize=new(360,155),FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(28,31,45)};
        var label=new Label{Text=creating?"You are creating this room. Choose its private key:":"Enter the private key to access this room:",ForeColor=Color.White,Left=18,Top=18,Width=325};
        var input=new TextBox{UseSystemPasswordChar=true,Left=18,Top=50,Width=325};var ok=new Button{Text=creating?"CREATE ROOM":"UNLOCK",DialogResult=DialogResult.OK,Left=218,Top=95,Width=125};
        form.Controls.AddRange([label,input,ok]);form.AcceptButton=ok;return form.ShowDialog(owner)==DialogResult.OK?input.Text:null;
    }
}
