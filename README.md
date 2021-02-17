# exchange-set-service

The Exchange Set Service is intended to allow distributors to request a custom S63 exchange set  built to the S-63 Specification, for delivery of requested ENC data required for vessels.

An exchange set: The set of files representing a complete, single purpose (i.e. product specific) data transfer. For example, the "ENC product specification" defines an exchange set which contains one Catalogue file and at least one Data Set file. An exchange set can be loaded into an ECDIS (Electronic Chart Display and Information System).

## Viewing the API Spec

Rather than reading the Spec in raw form, copy the content of the OpenAPI spec .yml file into https://editor.swagger.io/ for a nice rendered view

## Calling the Exchange Set Service

*Note that this  section is draft, based on what we are doing for FSS (see <https://github.com/UKHO/file-share-service/>).*

### Postman

To Generate an authorization token in postman follow the following steps:

#### Get Authorization for the Environments AppRegistration

Ask ### TBC ### (or service Owner if service is live) to give you Roles for the file share service app registration.
- `ESS-API-DEV`
- `ESS-API-QA`
- `ESS-API-LIVE`

#### Select OAuth2.0 in postman

In Postman select `Oauth 2.0` in the authorization tab.

![OAuth2 Postman Section](/Documentation/Images/PostmanAuthTabOauth.png)

#### Fill in the Oauth section, click get new access token, login from the popup window

Grant Type - Implicit

Auth URL - https://login.microsoftonline.com/9134ca48-663d-4a05-968a-31a42f0aed3e/oauth2/v2.0/authorize

Client ID:

- DEV ### TBC ###
- QA ### TBC ###
- LIVE ### TBC ###

Scope:

- DEV ### TBC ###/.default
- QA ### TBC ###/.default
- LIVE ### TBC ###/.default

NOTE: browser used to authenticate needs to allow popups

![OAuth2 Postman Section](/Documentation/Images/PostmanAuthTabOauthDetails.png)

#### Update the token

paste it into the `Access Token` field at the top of the postman Oauth2 section.
