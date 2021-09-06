import { Trend } from 'k6/metrics';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';
import { sleep } from 'k6';

const runTestProductVersion = require('./LoadTestForProductVersions.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./dataHelper.js');
const productVersionData_Small = dataHelper.GetProductVersionDataforSmallExchangeSet();
const productVersionData_Medium = dataHelper.GetProductVersionDataforMediumExchangeSet();
const productVersionData_Large = dataHelper.GetProductVersionDataforLargeExchangeSet();
const apiClient = require('./clientHelper.js');

let SmallExchangeSetCreationTrend = new Trend('SmallEssCreationtime');
let MediumExchangeSetCreationTrend = new Trend('MediumEssCreationtime');
let LargeExchangeSetCreationTrend = new Trend('LargeEssCreationtime');
let clientAuthResp = {};

export let options = {
    scenarios: {
        ESSCreationSmallExchangeSet: {
            exec: 'ESSCreationSmallExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 25,
            iterations: 161,
            maxDuration: '1h'
        },
        ESSCreationMediumExchangeSet: {
            exec: 'ESSCreationMediumExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 161,
            maxDuration: '1h'
        },
        ESSCreationLargeExchangeSet: {
            exec: 'ESSCreationLargeExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 1,
            iterations: 170,
            maxDuration: '1h'
        },
    },
};

export function setup() {
    // client credentials authentication flow
     let essAuthResp = authenticateUsingAzure(
         `${config.ESS_TENANT_ID}`, `${config.ESS_CLIENT_ID}`, `${config.ESS_CLIENT_SECRET}`, `${config.ESS_SCOPES}`, `${config.ESS_RESOURCE}`
     );
    clientAuthResp["essToken"] = essAuthResp.access_token;
    
     let fssAuthResp = authenticateUsingAzure(
         `${config.FSS_TENANT_ID}`, `${config.FSS_CLIENT_ID}`, `${config.FSS_CLIENT_SECRET}`, `${config.FSS_SCOPES}`, `${config.FSS_RESOURCE}`
     );
    clientAuthResp["fssToken"] = fssAuthResp.access_token;
    
    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('SmallEssCreation', () => {
        runTestProductVersion.ESSCreation(clientAuthResp, productVersionData_Small, "Small");
    });
    SmallExchangeSetCreationTrend.add(group_duration);
    sleep(1);
}

export function ESSCreationMediumExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('MediumEssCreation', () => {
        runTestProductVersion.ESSCreation(clientAuthResp, productVersionData_Medium, "Medium");
    });
    MediumExchangeSetCreationTrend.add(group_duration);
    sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('LargeEssCreation', () => {
        runTestProductVersion.ESSCreation(clientAuthResp, productVersionData_Large, "Large");
    });
    LargeExchangeSetCreationTrend.add(group_duration);
    sleep(1);
}

export function handleSummary(data) {
    return {
        "summary/result.html": htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        "summary/summary.json": JSON.stringify(data),
    }
}