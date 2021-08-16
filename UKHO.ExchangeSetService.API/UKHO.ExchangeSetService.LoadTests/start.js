import http from "k6/http";
import { authenticateUsingAzure } from './oauth/azure.js';
import runTestProductIdentifier from "./LoadTestProductIdentifierSmallExchangeSet.js";
import runTestProjectVersion from "./LoadTestProductVersionSmallExchangeSet.js";

const config = JSON.parse(open('./config.json'));

export let options = {
    vus: 1,
    duration: "2m",
    thresholds: {
        checks: ['rate>0.9'],
    },
};

export function setup() {
    // client credentials authentication flow

    let essAuthResp = authenticateUsingAzure(
        `${config.ESS_TENANT_ID}, ${config.ESS_CLIENT_ID}, ${config.ESS_CLIENT_SECRET}, ${config.ESS_SCOPES}, ${config.ESS_RESOURCE}`
    );

    let fssAuthResp = authenticateUsingAzure(
        `${config.FSS_TENANT_ID}, ${config.FSS_CLIENT_ID}, ${config.FSS_CLIENT_SECRET}, ${config.FSS_SCOPES}, ${config.FSS_RESOURCE}`
    );

    clientAuthResp = essAuthResp.concat(fssAuthResp);
    return clientAuthResp;
}

export default function (clientAuthResp) {
    runTestProductIdentifier(clientAuthResp);
    runTestProjectVersion(clientAuthResp);
};