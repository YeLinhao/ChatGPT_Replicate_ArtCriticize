import UnityEngine as ue
import replicate
import os
import openai
import time


f = open(ue.Application.dataPath + "/Python/ReplyText.txt","w")

os.environ["REPLICATE_API_TOKEN"] = "r8_bUx0naLV7NCJrYFmFiHdVDD1zw8AaEF2tetcV"

ue.Debug.Log("pythonSuccess")

result = replicate.run(
    "methexis-inc/img2prompt:50adaf2d3ad20a6f911a8a9e3ccf777b263b8596fbd2c8fc26e8888f8a0edbb5",
    input={"image": open(ue.Application.dataPath + "/CameraScreenshot.png","rb")}
)
ue.Debug.Log(result)
#f.write(result)


your_API_key = "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"
def openai_reply(words):
    openai.api_key = your_API_key
    response = openai.ChatCompletion.create(
        model="gpt-3.5-turbo",
        messages=[{"role": "user", "content": words}]
    )

    return response.choices[0].message['content']


ue.Debug.Log("ChatGPTEnter")

rule = "Suppose you are an art critic and have a painting," + result + "Please comment in a critical tone and do not wrap the paragraph"
output1 = openai_reply(rule)
ue.Debug.Log(output1)
f.write(output1)
f.flush()

ue.Debug.Log("ChatGPTSuccess")

f.close()