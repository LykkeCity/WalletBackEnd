var bitcore = require('bitcore')
var imported = bitcore.PrivateKey.fromWIF(process.argv[2]).toString();
var privateKey = new bitcore.PrivateKey(imported);
var exported = privateKey.toWIF();
console.log(exported);
