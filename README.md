## Changes in appsettings.json

1. **HmacPubSubHub Secret**: Update the secret for HmacPubSubHub as required.

2. **Callback Api Key**: Update the value for CallbackApiKey under secrets, which you use to talk to the api. 

3. **Callback** Value under **Hub**, url for the Pubsubhubbub
4. **Youtube Api Key**: which is used to get the video description from a video.
5. **OpenAIApiKey**: extract promotions via ChatGPT
6. **When Using Without Docker**:

   - Update `ConnectionStrings:Sqlite` to `"DB/PromoCodes.db"`.

   - Update `Path:Serilog` to `"logs/logfile.txt"`.

7. **If Using Docker-Compose**:

   - Ensure that you change the `"[path]"` value under volumes to an actual path on your operating system. For example, use `"/mnt/"` for Linux.
