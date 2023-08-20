## Changes in appsettings.json

1. **HmacPubSubHub Secret**: Update the secret for HmacPubSubHub as required.

2. **When Using Without Docker**:

   - Update `ConnectionStrings:Sqlite` to `"DB/PromoCodes.db"`.

   - Update `Path:Serilog` to `"logs/logfile.txt"`.

3. **If Using Docker-Compose**:

   - Ensure that you change the `"[path]"` value under volumes to an actual path on your operating system. For example, use `"/mnt/"` for Linux.
