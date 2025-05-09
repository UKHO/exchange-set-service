import { sleep, group } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './Oauth/Azure.js';

const runTestSinceDateTime = require('./Scripts/LoadTestForProductInformation.js');
const dataHelper = require('./Helper/DataHelper.js');
const config=JSON.parse(open('./config.json'));
const sinceDateTimeData_Large = dataHelper.GetSinceDateTimeDataForLargeExchangeSet();
const sinceDateTimeData= dataHelper.GetProductIdentifierDataForSinceDateTime();

let clientAuthResp = {};
let essResponse;

export let options = {
    scenarios: {
        MaxDateRangeResponse: {
            exec: 'MaxDateRangeResponse',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 30,
            maxDuration: '1h'
        }
    },
};

export function setup() {
    // client credentials authentication flow
    let essAuthResp = authenticateUsingAzure(
        `${config.ESS_TENANT_ID}`, `${config.ESS_CLIENT_ID}`, `${config.ESS_CLIENT_SECRET}`, `${config.ESS_SCOPES}`, `${config.ESS_RESOURCE}`
    );
    clientAuthResp["essToken"] = essAuthResp.access_token;

    return clientAuthResp;
}

export function MaxDateRangeResponse(clientAuthResp) {
    group('MaxDateRangeResponse', () => {        
        runTestSinceDateTime.DeltaSetResponse(clientAuthResp, sinceDateTimeData, sinceDateTimeData_Large, "Large");
    });
    sleep(1);
}

export function handleSummary(data) {
    return {
        ["summary/ProductInformationResult_"+new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]:htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        ["summary/ProductInformationResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]:JSON.stringify(data),
    }
}