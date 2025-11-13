import torch
print(torch.__version__, "CUDA:", torch.version.cuda, "avail:", torch.cuda.is_available())
print("capability:", torch.cuda.get_device_capability(0))



# import requests
# r = requests.post("http://127.0.0.1:5005/tts/wav",
#                   json={"text":"Bonjour, je suis Achraf.", "speed":1.0})
# open("out.wav","wb").write(r.content)