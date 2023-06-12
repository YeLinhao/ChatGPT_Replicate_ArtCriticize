# ChatGPT_Replicate_ArtCriticize
这是一个结合了ChatGPT与Replicate插件，基于Unity "2D/3D Painting"资产，以python和C#编写，能够在unity中运行的对于玩家画图作品进行评价的交互原型  
This is a Interactable demo combined with ChatGPT and Replicate,and based on Unity Asset"2D/3D Painting" and be written by C# and python, achieving the function that NPC comment to player's artwork.
 
 **Background**
 有越来越多的游戏将“玩家创意”作为核心玩法，但系统无法模拟人类对“创意”做出合理的评分、奖励，当前的解决方案多为从语料库中随机拼合，使游戏环节缺失正反馈。

 **Meaning**  

1.突破了游戏系统对玩家创意进行评价的限制；
2.补全了该类游戏的正反馈体验；
3.GPT结合专业知识的反馈，甚至可以寓教于乐，让玩家真正学习到应该如何画得更好；

 
 **Setup**  
 
 Step1.解压该文件至您电脑上通常存放Unity工程的文件夹。  Unzip the file to the folder on your computer where the Unity project usually resides.  
 
 Step2.运行Unity Hub, 点击“Open”按钮，选择并打开该文件夹。 Run Unity Hub, click the "Open" button, select and open the folder.  
 
 Step3.获取ChatGPT与Replicate的APIkey，分别位于以下网址：https://platform.openai.com/account/api-keys , https://replicate.com/account/api-tokens. Get the APIkeys for ChatGPT and Replicate, respectively at the following urls: https://platform.openai.com/account/api-keys , https://replicate.com/account/api-tokens  
 
 Step4. 打开Assets/python，将自己的APIkey替换至python脚本“main_positive.py”,"main_neutral.py","main_negative.py"中，ReplicateAPI位于第10行，ChatGPTAPI位于第25行。  
 
 Step5.在UnityEditor中运行它，绘制你的杰作，截图并让GPT给你评论! Run it in UnityEditor,make your masterpiece,take a screenshot and let GPT give you criticize!

 **Limit**  
 由于Replicate,ChatGPT的接口返回值需要时间，因此在进行评论后，需要耐心等待1min左右。Replicate takes time to return the value of the ChatGPT interface, so you need to wait about 1 minute after commenting.
