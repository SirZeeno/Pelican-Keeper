if [[ {{AutoUpdate}} == "1" ]]; then 
  bash update.sh; 
fi; # Do I need this even, I am already doing the update checking and the update bash script in the program itself
chmod +x "Pelican Keeper"; # I can do this during install, no need to do it every time I boot up the bot
./"Pelican Keeper";