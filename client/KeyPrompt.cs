namespace SigmaGame;
static class KeyPrompt
{
    public static string? Ask(Form owner,bool creating)
    {
        using var form=new Form{Text=creating?"Create security PIN":"Security PIN required",ClientSize=new(360,155),FormBorderStyle=FormBorderStyle.FixedDialog,StartPosition=FormStartPosition.CenterParent,MaximizeBox=false,MinimizeBox=false,BackColor=Color.FromArgb(28,31,45)};
        var label=new Label{Text=creating?"As room owner, choose a 4–8 digit security PIN:":"Enter the owner's 4–8 digit security PIN:",ForeColor=Color.White,Left=18,Top=18,Width=325};
        var input=new TextBox{UseSystemPasswordChar=true,Left=18,Top=50,Width=325};var ok=new Button{Text=creating?"CREATE ROOM":"UNLOCK",DialogResult=DialogResult.OK,Left=218,Top=95,Width=125};
        input.MaxLength=8;form.Controls.AddRange([label,input,ok]);form.AcceptButton=ok;if(form.ShowDialog(owner)!=DialogResult.OK)return null;return input.Text.Length is >=4 and <=8&&input.Text.All(char.IsDigit)?input.Text:null;
    }
}
