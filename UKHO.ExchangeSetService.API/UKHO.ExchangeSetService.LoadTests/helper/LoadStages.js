const logParser = require('./LogParser.js');
/**
 * @param {Object} IterationCount The number of iteration to be distributed 
 * @returns {Array} Request rate per stage required to complete the iterations  
*/
export function getLoadStages(LogFile) {
    let reqRate = logParser.requestRatePerMinute(LogFile);
    const testStages = Object.values(reqRate);
    return testStages;
}

export default function myMain() {
    let stages = getLoadStages(6125)
    console.log(stages)
}