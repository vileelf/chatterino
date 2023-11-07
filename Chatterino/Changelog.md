# changelog

# 3.4.1
-- Switched 7tv global emotes over to the newer api. Fixes them not loading either.

# 3.4
-- Switched over to 7tvs newer api. Fixes them not loading again.
-- Fixed wide zero width emotes going too far to the left and covering users names.
-- Made it so channel emotes have priority over global emotes for display.
-- Fixed elevated chat messages displaying values multiplied by 100.
-- Fixed changelog showing every time even though the user has seen it already.
-- Fixed a rare bug where the cache can get corrupted and cause Chatterino to crash on launch.

# 3.3.2
-- Fixed 7tv emotes not loading due to them changing their api.

## 3.3.1
-- Switched over to use twitches new api handles for badges since the old ones died.

## 3.3
-- Added a reload all channels button to the dropdown.
-- Updated commands to use the twitch api. All old commands should be functional again. 
-- Also added commands for streamers to do predictions. use /help prediction for more info.
-- Twitch killed the get chatters endpoint so now we no longer have a proper list of chatters. As such name complete will only include people that have chatted since you opened chatterino and wont refresh.
-- Cleaned up the login form a bit.

## 3.2.1
-- fixed bug preventing 7tv emotes from loading.

## 3.2
-- fixed bug where ffz emotes wouldn't display anymore. 
-- Updated appsettings, commands, and layout to be saved when changes are made and not on closing.
-- Updated bit emotes to include the name in the tooltip. 
-- Fixed bug causing messages to duplicate sometimes on rejoin. 
-- If the users name on user info popup is different from the name in the chat it now shows that.

## 3.1.2.1
-- Fixed a bug with 7tv global emotes not loading.
-- Fixed a bug with the updater not working in some instances.

## 3.1
-- Added support for some more emojis.
-- Chatterino should keep trying to reconnect when disconnected.
-- Emote cache can now unload emotes that aren't used for a while.
-- Fixed emote bugs due to twitches changes to follower emotes.
-- Removed code that gives errors for sending messages too quickly.

## 3.0
-- Added support for elevated chat messages.
-- Added support for most modern emojis except for some flags.
-- Added double click to highlight word on the input box.
-- Updated the search dialog with next and prev buttons.
-- Added an automatic updater with patch notes and the option to skip the update.

## 2.9
-- Added more permissions to the login to allow for mod commands like /host and /commercial and fix some stuff not working anymore like whispers.
-- Hovering over the sub badge will now show the number of months subbed.
-- Added a menu option to display who's live from your follow list.
-- Added support for the new announcements feature twitch added.
-- Fixed a bug where 7tv emotes would show up as the wrong alias in some conditions.
-- Added notification options for when a tab goes live. you can add a custom sound for it and flash the taskbar.
-- Added a reload global emotes menu option.
-- Added support for deleting messages. You can delete them by clicking on a username on the message you wanna delete and then clicking delete.
-- Added several missing commands to command tab complete list.

## 2.8
- switched everything over to using the new helix api.
- switched over to using the automatic login. this change will require you to relogin sorry.
- fixed instances where the user info popup would go off screen.
- the search box will now autofill with the current selected text.

## 2.7
- added a rejoin channel option to the drop down menu and a /rejoin command that rejoins the current channel without reconnecting the whole program.
- added a new setting to disable/enable double clicking a tab to change the tab title.
- Now joining channels at a slower rate to match twitches new join speed limits.
- added support for 7tv emotes

## 2.6
- using newer api for getting user emotes. It should include bit emotes and animated emotes now
- added support for follower emotes
- added a new dropdown option show channel emotes. It shows all available twitch emotes for the current channel
- when you change the scale of emotes you can now use reload emotes to update the resolution of the emotes for the current channel
- Bug Fixes
- fixed a bug where the input wouldn't shrink down after decreasing the font to a size below what was used when chatterino was launched
- bttv and ffz emotes are now loaded from the cache before recent messages are retrieved so they will now show up when chatterino is first launched.
- fixed a bug with autocomplete where if you did an autocomplete that went to a new line it wouldn't let you tab through the options like normal
- fixed a bug where messages would get stuck when you change tabs when using the hide input if empty option

## 2.5
- fixed an issue with the open pop out player button
- images on tooltips are now centered
- Added support for twitches new animated emotes

## 2.4
- added a default font button to the settings next to select font
- pressing control+f now displays a dialog for the search function rather than just using the chat input box
- added a new command /user {username}. It will display the user popup for that user
- fixed a bug causing custom mod badges to be overscaled
- Made it so custom mod badge tooltips can show the larger version if it exists
- added support for custom ffz vip badges
- fixed a bug preventing the anon bit emote from showing
- renaming from chatterino to chatterino classic

## 2.3
- added a new feature that lets you make notes about a user. Simply click their name, click notes, and type whatever you please. Notes arnt dependent on username so they should stay there when they change names.
- fixed an issue that caused tabs to disappear sometimes when you launched chatterino. 
- fixed an issue causing emotes to display the wrong name sometimes
- fixed an issue with the recent messages api that caused some messages to show up blank
- reload emotes now will also update the sub badges and cheer badges

## 2.2 hotfix
- Fixed an issue where the autocomplete would cause chatterino to freeze up.

## 2.2
- Added a new feature that autocompletes users when you type @ and emotes when you type :

## 2.1
- showing username on both the gifter and giftee of sub gifts
- added an option to set the message limit
- fixed a bug where the tooltip would go behind other windows so you couldn't see it

## 2.0
- now displays username on sub notifications if it is different from the display name.
- gif emotes are now synced up with each other.
- there's a new option that displays bigger pictures (if available) of emotes in the tooltips you get from mousing over them.
- added an option for a recently used emote list that keeps track of emotes you use that aren't part of your emote list.
- added cvmask and cvhazmat to the list of hat emotes
- added an option for emote caching that stores emotes when they are downloaded so you don't have to download them all again when you relaunch chatterino.
- emotes will try to load again eventually if they fail the first time
- new right click menu that lets you right click to copy or append to the chat box
- added an option for highlighting highlighted messages

## 1.9
- you can now tab complete using @ in commands
- added the ability to double click a word to select it
- fixed a bug causing you to lose your selection whenever there's a new message
- recombined reload channel and reload subemotes
- removed reload username list button
- subs and sub gifts are now added to the username tab complete list
- fixed issue causing some gifs to be too fast
- fixed issue preventing login for users with an _ in their name
- made gif emotes only animate when they are on screen
- changed the color of highlighted messages and removed them from the scrollbar
- search results now have highest priority for highlighting.
- made chatterino run much more efficiently
- updated message queue limit from 50 to 500 so you should get that ignored x messages less

## 1.8
- added follower count and streamer type to the user info popup 
- added the start of a search feature. Type in the chatbox and press ctrl+f to highlight any match. remove all text and press ctrl+f again to clear the search highlights
- added missing "hat" emotes to the hardcoded list. So things like SoSnowy should work
- added support for the highlighted messages feature from the new twitch points system
- updated to use the new bttv api so bttv emotes load again

## 1.7 hotfix
- fixed an issue preventing chatterino from finding the twitch channel id

## 1.7
- fully converted chatterino over to the twitch api v5
- readded in the recent chat feature using third party api used by chatterino 2
- fixed the follow button to actually follow when clicked
- fixed the add a space for duplicate message feature

## 1.6 hotfix 2
- converted sub emotes and check if live over to twitch api v5.

## 1.6 hotfix
- added support for twitches new modified emotes
- disabled the check for updates functionality

## 1.6
- added tab highlighting for when a channel goes live and a channel setting for it
- added a new setting to disable the x buttons on tabs
- added a menu item to manually reload sub emotes and one to reload usernames
- fixed time displayed on sub messages
- added a new setting to ignore users using the twitch api vs chatterinos internal list

## 1.5
- added a new setting for username based highlighting
- now recognizes vip for fast messaging
- now properly respecting the setting for sending duplicate messages
- overloaded the reload channel emotes button to also reload your other emotes.

## 1.4
- added support for all bit emotes
- added support for all badges
- added support for custom cheer badges

## 1.3
- fixed the LSD bug (entire screen filling up with text that never gets cleared)
- changed access to the ffz api according to their changes

## 1.2.13
- updated to the .net framework 4.6.1
- fixed twitch emote images not loading
- added own implementation of ignored users instead of using the twitch api

## 1.2.12
- added optional ban and custom timeout buttons (sponsored by Wipz)

## 1.2.11
- fixed some arabic character crashing DirectWrite

## 1.2.10
- fixed an issue that caused chatterino to crash when it received invalid emote data from twitch

## 1.2.9
- fixed an issue with commands when sending duplicate messages

## 1.2.8
- added badges for 25000, 50000 and 75000 bits
- combined disconnecting and reconnecting message into one
- tweaked code a bit so it doesn't disconnect on my unis wifi as much

## 1.2.6
- added the twitch verified partner badge
- fixed chat being invisible when a global moderator is in chat
- fixed the moderator dropdown not showing

## 1.2.5
- added an option to prefere emotes over usernames when tab-completing
- fixed some username colors looking weird
- fixed streamlink quality options for streams that use "720p,480p,..." instead of "high,medium,..." as their quality options
- added custom arguments for streamlink
- fixed message limit not changing after being modded/unmodded
- made ctrl+enter send messages every 1.6 seconds if you are not mod

## 1.2.4
- added streamlink support (thanks to cranken1337)
- fixed an issue that caused bttv and ffz global emotes not to load for some users
- made username colors more vibrant
- changed colors for highlights, whispers and resubs

## 1.2.3
- fixed crash when closing splits
- fixed sending whispers from /whispers and /mentions
- fixed the very important typo in the settings

## 1.2.2
- added option for rainbow username colors
- made the "jump to bottom" more obvious
- fixed the reconnecting issue

## 1.2.1
- fixed text being copied twice

## 1.2
- fixed channel ffz emotes being tagged as "global"
- added ap/pm timestamp format
- added "new" cheer badges

## 1.1
- fixed window size resetting to 200x200 px on start

## 1.0.9
- fixed an issue preventing users from starting chatterino
- fixed the icon having a santa hat (when you restart your pc/clean icon cache)

## 1.0.8
- added /r which expands to /w <last user you whispered with>
- added support for ffz emote margins
- chatterino now uses the proper 2x and 4x emotes for ffz and bttv

## 1.0.7.1
- temporarily disabled SoSnowy again because it was causing lag

## 1.0.7
- fixed gif emotes with hats
- fixed hat emotes going over others in the emote list

## 1.0.6
- added support for the bttv christmas emotes (unfortunately SoSnowy does not work)

## 1.0.5
- added emote scaling
- added live indicator to splits
- added button in the user info popup to disable/enable highlights for certain users
- added option to show messages from ignored users if you are mod or broadcaster

- fixes the user info popup going over the screen workspace
- fixed shift + arrow keys not selecting text by characters
- fixed not parting channel when closing split
- fixed copying spaces after emojis

## 1.0.4
- fixed the messages appearing multiple times after switching accounts

## 1.0.3
- disabled hardware acceleration to take less performance when playing games
- now also shows outgoing whispers in chat when inline whispers are enabled
- some messages now don't highlight tabs anymore
- emote list now gets brought to front when you click the button again
- added option to reload channel emotes without restarting
- timeout messages are now bundled

## 1.0.2
- fixed the broken updater, sorry for the inconvenience NotLikeThis

## 1.0.1
- fixed cache being saved to the wrong directory causing bttv emotes not to show

## 1.0
- moved all the settings to %appdata%
- added support for multiple accounts (aka the feature nobody asked for)
- added login via fourtf.com for users that can't open a tcp port
- added /mentions tab (thanks to pajlada)
- fixed gif emotes with 0s frames crashing chatterino
- /whispers got updated

## 0.3.1.1
- added ffz event emotes

## 0.3.1.0
- added option to make the window top-most
- added loyalty subscriber badges
- fixed cheers split up in multiple words
- fixed backgrounds for custom mod badges
- fixed spacing when switching fonts
- improved mouse wheel scrolling very long messages

## 0.3.0.3
- fixed subscriber badges not showing up
- fixed timeout button in the user popup

## 0.3.0.2
- disabled mentioning with @ in commands

## 0.3
- added a slider for the mouse scroll speed
- added option for a manual reconnect
- added a popup when you click on a name
- added an option to ignore messages
- added a rightclick context menu for links
- added an option to mention users with an @
- added twitch prime badge
- fixed emotes in sub messages
- fixed the "gray thing"

## 0.2.6.4
- fixed sub badges not showing up

## 0.2.6.3
- fixed commands not updating when one is deleted

## 0.2.6.2
- added CTRL + 1-9 to select tabs
- added ALT + arrow keys to switch tabs
- added ignored users
- added a settings for the message length counter
- added an emote list popup
- added an option to change the hue of the theme
- changed preferences so all the changes are immediate and cancel reverts them
- fixed tabing localized names
- removed 1 hour emote cache
- tweaked global bad prevention

## 0.2.3
- added FFZ channel emotes
- added a message length counter
- added custom commands
- added a donation page https://streamtip.com/t/fourtf
- fixed timeouts not being displayed sometimes
- fixed ctrl + backspace not deleting a word for some users

## 0.2.2
- added x button to tabs
- add option to disable twitch emotes

## 0.2
- added tabs
- added 4 themes (white and light are still work-in-progress)
- added an option to seperate messages
- added a filter for emotes
- added cheerxxx emotes
- added arrow up/down for previous/next message
- added mouse text selection in the input box

## 0.1.5
- added ratelimiting for messages
- added the option to ignore highlights from certain users
- fixed emote/name tabbing when no completion is available
- fixed subs/resubs
- fixed timeouts not showing up
- fixed name links not being clickable
- fixed a graphics issue with extremely high windows
- esc now closes some dialogs

## 0.1.4
- added twitch bit cheer badges
- replaced irc library with my own irc implementation
- fixed some notices not showing up

## 0.1.3
- added setting to change font
- added custom highlight sounds
- added keyboard shortcuts: ctrl+x (cut text), ctrl+enter (send message without clearing the input), end + home (move to start / end of input)
- added direct write support
- improved performance of word-wrapping
- updated icon (thanks to SwordAkimbo)

## 0.1.2
- fixed text caret disappearing
- improved animated gif draw performance

## 0.1.1
- added a changelog viewer
- made text input prettier
