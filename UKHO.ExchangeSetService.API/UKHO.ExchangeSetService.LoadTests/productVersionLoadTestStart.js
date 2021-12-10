import { htmlReport } from "https://raw.githubusercontent.com/benc-uk/k6-reporter/main/dist/bundle.js";
import { textSummary } from "https://jslib.k6.io/k6-summary/0.0.1/index.js";
import { authenticateUsingAzure } from './oauth/azure.js';
import { sleep, group } from 'k6';

const runTestProductVersion = require('./scripts/LoadTestForProductVersions.js');
const config = JSON.parse(open('./config.json'));
const dataHelper = require('./helper/dataHelper.js');
const productVersionData_Small = dataHelper.GetProductVersionDataforSmallExchangeSet();
const productVersionData_Medium = dataHelper.GetProductVersionDataforMediumExchangeSet();
const productVersionData_Large = dataHelper.GetProductVersionDataforLargeExchangeSet()
let clientAuthResp = {};

export let options = {
    scenarios: {
        ESSCreationSmallExchangeSet: {
            exec: 'ESSCreationSmallExchangeSet',
            executor: 'per-vu-iterations',
            startTime: '10s',
            gracefulStop: '5s',
            vus: 5,
            iterations: 400,
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

    return clientAuthResp;
}

export function ESSCreationSmallExchangeSet(clientAuthResp) {
    group('SmallEssCreation', () => {
        runTestProductVersion.ESSCreation(clientAuthResp, productVersionData_Small, "Small");
    });
    sleep(1);
}

export function ESSCreationMediumExchangeSet(clientAuthResp) {
    group('MediumEssCreation', () => {
        runTestProductVersion.ESSCreation(clientAuthResp, productVersionData_Medium, "Medium");
    });
    sleep(1);
}

export function ESSCreationLargeExchangeSet(clientAuthResp) {
    group('LargeEssCreation', () => {
        runTestProductVersion.ESSCreation(clientAuthResp, productVersionData_Large, "Large");
    });
    sleep(1);
}

export function handleSummary(data) {
    return {
        ["summary/ProductVersionResult_"+new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".html"]:htmlReport(data),
        stdout: textSummary(data, { indent: " ", enableColors: true }),
        ["summary/ProductVersionResult_" + new Date().toISOString().substr(0, 19).replace(/(:|-)/g, "").replace("T", "_") + ".json"]:JSON.stringify(data),
    }
}