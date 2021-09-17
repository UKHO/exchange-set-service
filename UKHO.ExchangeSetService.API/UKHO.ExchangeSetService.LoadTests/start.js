import http from "k6/http";
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';
import { sleep } from 'k6';
import { Trend } from 'k6/metrics';

const runTestProductIdentifier = require('./LoadTestForProductIdentifier.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./dataHelper.js');
const productIdentifierData_Small_25MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_25MB();
const productIdentifierData_Small_50MB = dataHelper.GetProductIdentifierDataforSmallExchangeSet_50MB();
const productIdentifierData_Medium_150MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_150MB();
const productIdentifierData_Medium_300MB = dataHelper.GetProductIdentifierDataforMediumExchangeSet_300MB();
const productIdentifierData_Large = dataHelper.GetProductIdentifierDataforLargeExchangeSet();

let SmallExchangeSetCreationTrendfor25MB = new Trend('SmallEssCreationtimefor25MB');
let SmallExchangeSetCreationTrendfor50MB = new Trend('SmallEssCreationtimefor50MB');
let MediumExchangeSetCreationTrendfor150MB = new Trend('MediumEssCreationtimefor150MB');
let MediumExchangeSetCreationTrendfor300MB = new Trend('MediumEssCreationtimefor300MB');
let LargeExchangeSetCreationTrend = new Trend('LargeEssCreationtime');
let clientAuthResp = {};

export let options = {
    scenarios: {
        ESSCreationSmallExchangeSet_25MB: {
            exec: 'ESSCreationSmallExchangeSet_25MB',
            executor: 'constant-arrival-rate',
            rate: 133,
            timeUnit: '1m',
            duration: '1h',
            preAllocatedVUs: 245,
            maxVUs: 270,
        },
        ESSCreationSmallExchangeSet_50MB: {
            exec: 'ESSCreationSmallExchangeSet_50MB',
            executor: 'constant-arrival-rate',
            rate: 26,
            timeUnit: '1m',
            duration: '1h',
            preAllocatedVUs: 48,
            maxVUs: 50,
        },
        ESSCreationMediumExchangeSet_150MB: {
            exec: 'ESSCreationMediumExchangeSet_150MB',
            executor: 'constant-arrival-rate',
            rate: 27,
            timeUnit: '5m',
            duration: '1h',
            preAllocatedVUs: 22,
            maxVUs: 25,
        },
        ESSCreationMediumExchangeSet_300MB: {
            exec: 'ESSCreationMediumExchangeSet_300MB',
            executor: 'constant-arrival-rate',
            rate: 8,
            timeUnit: '10m',
            duration: '1h',
            preAllocatedVUs: 3,
            maxVUs: 4,
        },
        ESSCreationLargeExchangeSet: {
            exec: 'ESSCreationLargeExchangeSet',
            executor: 'constant-arrival-rate',
            rate: 8,
            timeUnit: '10m',
            duration: '1h',
            preAllocatedVUs: 3,
            maxVUs: 4,
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

export function ESSCreationSmallExchangeSet_25MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Small_25MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Small_25MB, "Small_25MB");
    });
    SmallExchangeSetCreationTrendfor25MB.add(group_duration);
    sleep(1);
}

export function ESSCreationSmallExchangeSet_50MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Small_50MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Small_50MB, "Small_50MB");
    });
    SmallExchangeSetCreationTrendfor50MB.add(group_duration);
    sleep(1);
}

export function ESSCreationMediumExchangeSet_150MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Medium_150MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Medium_150MB, "Medium_150MB");
    });
    MediumExchangeSetCreationTrendfor150MB.add(group_duration);
    sleep(1);
}

export function ESSCreationMediumExchangeSet_300MB(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('Medium_300MB_EssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Medium_300MB, "Medium_300MB");
    });
    MediumExchangeSetCreationTrendfor300MB.add(group_duration);
    sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    var group_duration = apiClient.GetGroupDuration('LargeEssCreation', () => {
        runTestProductIdentifier.ESSCreation(clientAuthResp, productIdentifierData_Large, "Large");
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