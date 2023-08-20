Change in appsettings.json:

1. the secret for HmacPubSubHub
2. When using without docker
   2.1 ConnectionStrings:Sqlite to "DB/PromoCodes.db" 
   2.2 Path:Serilog to "logs/logfile.txt"
3. If using docker-compose make sure to change the "[path]" value under volumes to an actual path on your os like "/mnt/" for linux
