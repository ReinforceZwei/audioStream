# Audio Stream

Stream all sound from your computer to other devices (such as your phone), i.e. use your phone as computer speaker.

This project is made for fun only. If you want to try it out, you have to build it yourself.

The stream will have around 1 second delay.

# Before building

You have to edit `program.cs` and add your own computer IP address to it. Otherwise only localhost can connect to the server.

# How to use

Turn off firewall, or add firewall rule allowing incoming port 8080.

Start the program with admin privilege (IDK why built-in `HttpListener` requires admin, not even using privileged ports).
If you feel uncomfortable granting it admin privilege, you may replace the built-in `HttpListener` if you know how.

Use **other devices** to open `http://<your-pc-ip>:8080/` in the browser and play the stream.

If you play the stream on the same computer you will have caused audio loopback and might blow up your ears.

# For iOS user

Safari cannot play the stream. Download VLC player to play the stream with URL `http://<your-pc-ip>:8080/audio`
