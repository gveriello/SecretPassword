using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities
{
    public class Credential
    {
        private string _passwordVisual = string.Empty;
        private bool _isPasswordHidden = true;
        public int ID { get; set; }
        public string Title { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PasswordVisual { get; set; }
        public bool ShowPassword
        {
            get { return _isPasswordHidden; }
            set
            {
                _isPasswordHidden = value;
                if (this._isPasswordHidden)
                    this.PasswordVisual = this.Password;
                else
                    this.PasswordVisual = "**********";
            }
        }
        public string Url { get; set; }
        public string Notes { get; set; }
        public DateTime? Expires { get; set; }
        public int? GroupID { get; set; }
        public int UserID { get; set; }
    }
}
