# Security

To make a call into the HTTP API, you will need an access code.  This is because anonymous calls are 
switched off for this app. 

To get the key, go to the Azure Function, and in the App Keys section either create one or pick the Default
key.  

The URL to call into the app will then be something like this (this isn't the real key):

    https://azdnsrg-prod.azurewebsites.net/api/update?code=PyDaTrKk7fjbP6VbgzjhxY5y3/webSaTNDE4L06xnvzvZjJVER3o/g==&zone=cluster8.tech&group=developmentrg&reqip=__MYIP__&name=%40

# Deployment
Entirely via DevOps, including approval gate and remote state.