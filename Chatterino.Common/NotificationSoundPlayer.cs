using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Media;
using System.IO;



namespace Chatterino.Common
{
    public class NotificationSoundPlayer
    {
        private DateTime soundFileTimeStamp = DateTime.MinValue;
        private string soundPath = "";
        private SoundPlayer notificationSound;
        
        public NotificationSoundPlayer() {
        }
        
        public NotificationSoundPlayer(string soundFilePath) {
            soundPath = soundFilePath;
        }
        
        public void SetPath(string soundFilePath) {
            soundPath = soundFilePath;
        }
        
        public void Reload() {
            soundFileTimeStamp = DateTime.MinValue;
        }
        
        public bool Play()
        {
            
            SoundPlayer player = null;
            try
            {
                var fileInfo = new FileInfo(soundPath);
                if (fileInfo.Exists)
                {
                    if (fileInfo.LastWriteTime != soundFileTimeStamp)
                    {
                        notificationSound?.Dispose();
                        soundFileTimeStamp = fileInfo.LastWriteTime;
                        try
                        {
                            using (var stream = new FileStream(soundPath, FileMode.Open))
                            {
                                notificationSound = new SoundPlayer(stream);
                                notificationSound.Load();
                            }

                            player = notificationSound;
                        }
                        catch (Exception e)
                        {
                            notificationSound.Dispose();
                            notificationSound = null;
                            GuiEngine.Current.log(e.ToString());
                        }
                    } else {
                        player = notificationSound;
                    }
                }
            }
            catch (Exception e){ 
                GuiEngine.Current.log(e.ToString() + " " + soundPath);
            }
            try
            {
                if (player == null)
                {
                    return false;
                }

                player.Play();
                return true;
            }
            catch (Exception e){ 
                GuiEngine.Current.log(e.ToString());
            }
            return false;
        }
    }
}
