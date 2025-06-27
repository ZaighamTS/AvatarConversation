> Add glTFast using Unity Package Manager (After)

Step-by-step:
1. Open Unity
2. Go to Window > Package Manager
3. Click the + button → choose "Add package from Git URL..."
4. Paste this exact line:
	https://github.com/atteneder/glTFast.git
5. Click Install/Add

--------------------------------------------------------------------

> Add Custom/ReadyPlayerMe Character

As a default it will be a custom arab character in the app to add another custom character
you will need to go to (Scene)Asset\ChatBot\ChatbotScene -> (GameObject)RPM Player ->
go to (component)ThirdPersonLoader

Add your own avatar to the Preview Avatar with the animation controller attached in that component(ConversationAnimator)

If you want to add ReadyPlayerMe Avatar, add the Avatar URL e.g: https://models.readyplayer.me/6853f803baefb4ff71cf7dc0.glb
and turn the "Load On Start" toggle ON.

--------------------------------------------------------------------

IMPORTANT NOTE:
To activate OpenAI, you need to create a txt file in Assets/Resources folder and name it "openai_key.txt" (if not already present)
Paste the OpenAI API Key in there and save it.
Because the API key needs to be a secret DO NOT UPLOAD THE KEY ON GITHUB or it will be deleted or banned
Add the "openai_key.txt" in .gitignore
Place the following the the .gitignore file:

# Ignore API key in Resources
Assets/Resources/openai_key.txt
Assets/Resources/openai_key.txt.meta

Save it.
Make sure the txt file isn't being uploaded on git.
!!!!DO NOT PASTE THE OpenAI API Key ANY WHERE ELSE!!!!