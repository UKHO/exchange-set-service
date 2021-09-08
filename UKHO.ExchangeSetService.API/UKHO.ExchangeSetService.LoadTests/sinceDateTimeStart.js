import { sleep, group } from 'k6';
import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';

const runTestSinceDateTime = require('./LoadTestForSinceDateTime.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./dataHelper.js');

const sinceDateTimeData_Small = dataHelper.GetSinceDateTimeDataForSmallExchangeSet();
const sinceDateTimeData_Medium = dataHelper.GetSinceDateTimeDataForMediumExchangeSet();
const sinceDateTimeData_Large = dataHelper.GetSinceDateTimeDataForLargeExchangeSet();

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
    clientAuthResp["essToken"] = essAuthResp;

    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet(clientAuthResp) {
    group('SmallEssResponse', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Small, "Small");
    });
    sleep(1);
}

export function ESSCreationMediumExchangeSet(clientAuthResp) {
    group('MediumEssResponse', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Medium, "Medium");
    });
    sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    group('LargeEssResponse', () => {
        runTestSinceDateTime.ESSCreation(clientAuthResp, sinceDateTimeData_Large, "Large");
    });
    sleep(1);
}

export function handleSummary(data) {
    return {
        "summary/result.html": htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        "summary/summary.json": JSON.stringify(data),
    }
}