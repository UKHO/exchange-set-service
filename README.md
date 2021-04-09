# exchange-set-service

The Exchange Set Service will allow distributors to request custom exchange sets, built to the [S-63 specification](https://en.wikipedia.org/wiki/S-63_(encryption_standard)) for delivery of ENC data to vessels.

An exchange set is defined in the [IC-ENC Glossary](http://www.ic-enc.org/Glossary.html) as:

> An exchange set is a grouping of data sets in a logical, consistent and self-contained collection to support the interchange of geospatial data and meta data.

Read more in the full [IHO S-63 Specification](https://iho.int/uploads/user/Services%20and%20Standards/ENC_ECDIS/data_protection/S-63_e1.2.0_EN_Jan2015.pdf).

An exchange set can be loaded into an ECDIS (Electronic Chart Display and Information System) on a vessel for navigation planning.

## Viewing the API Spec

Rather than reading the OpenAPI specification in raw form, copy the API specification YAML file into [an online Swagger editor](https://editor.swagger.io/) for a rendered view.

## Calling the Exchange Set Service

*Note that this section is draft, based on what we are doing for the [File Share Service (FSS)](https://github.com/UKHO/file-share-service/)*

### Using ESS in Postman

To generate an authorization token in Postman follow these steps:

#### Get Authorization for the environment's App Registration

Ask ### TBC ### (or service Owner if service is live) to give you Roles for the ESS App Registration:

* `ESS-API-DEV`
* `ESS-API-QA`
* `ESS-API-LIVE`

#### Select OAuth2.0

Select `Oauth 2.0` in the authorization tab.

![OAuth2 Postman Section](/Documentation/Images/PostmanAuthTabOauth.PNG)

#### Fill in the OAuth section, click get new access token, login from the popup window

Grant Type - Implicit

Auth URL - <https://login.microsoftonline.com/9134ca48-663d-4a05-968a-31a42f0aed3e/oauth2/v2.0/authorize>

Client ID:

* DEV ### TBC ###
* QA ### TBC ###
* LIVE ### TBC ###

Scope:

* DEV ### TBC ###/.default
* QA ### TBC ###/.default
* LIVE ### TBC ###/.default

NOTE: The browser used to authenticate needs to allow popups!

![OAuth2 Postman Section](/Documentation/Images/PostmanAuthTabOauthDetails.PNG)

#### Update the token

Paste the token into the `Access Token` field at the top of the Postman OAuth2 section.
