const k = 42;
const inputs = [
    'mailto:contact@pawel-seweryn.pl',
    'contact@pawel-seweryn.pl',
    '+48 796 561 084 · Tychy, Poland',
    'https://github.com/seruss',
    'https://www.linkedin.com/in/pawe\u0142-seweryn-4677b7106/'
];
for (let s of inputs) {
    let arr = [];
    for (let i = 0; i < s.length; i++) {
        arr.push(s.charCodeAt(i) ^ k);
    }
    console.log('---');
    console.log(s);
    console.log(JSON.stringify(arr));
}
