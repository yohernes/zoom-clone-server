server for the zoom clone client repo. using TCP and UDP.
WinForms desktop app for online conferencing. LAN only (no hole punching)
# architecture
![image](https://github.com/user-attachments/assets/f75c807e-c455-4cdb-898c-ee51de2a08c5)


## communications  

audio, video streaming uses UDP protocol.
    
login, registration, creating a new meeting, joining meeting, and chat use TCP protocol.
  
## encryption  
 
RSA -> AES:
    
exchanging RSA keys to set AES key, which is used for all further communication.


  
# notes 
  
change the server IP in the "Program.cs" class to the correct one, and change the database data source string to the correct one.

if the database doesn't show up, create a new one based on the parameters in "Client.cs" class.


