# starter-authentication-ldap-webapi

## Starter Project for simple Token/Bearer Authentication Using LDAP


### Sample : Generate Token
#### End-point :
#### http://localhost:64384/v1/accounts/auth

#### Body:
#### {
	"clientid":"123",
	"username":"bdarley",
	"password":"password",
	"granttype":"password",
	"clientsecret":"secret"
}

### Sample : Response
#### {
    "code": "999",
    "message": "OK",
    "data": {
        "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc....",
        "expiresIn": 1800,
        "refreshToken": "435969c7c85f4048999e1036674b8337"
    }
}



### Sample : Refresh Token

#### End-point :
#### http://localhost:64384/v1/accounts/auth
#### Body:
#### {
		"clientId": "123",
		"granttype":"refreshToken",
		"refreshToken": "41d55c2ea3a3456fb555713720151aed"
}

### Sample : Refresh Token Response 
#### {
    "code": "999",
    "message": "OK",
    "data": {
        "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHR.....",
        "expiresIn": 120,
        "refreshToken": "9852e47557ab42a3916e6b3d98d1747c"
    }
}
