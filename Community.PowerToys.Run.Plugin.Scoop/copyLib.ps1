# this script uses [gsudo](https://github.com/gerardog/gsudo)

Push-Location
Set-Location $PSScriptRoot

sudo {
	$ptPath = "C:\Program Files\PowerToys"

    mkdir ./Lib

	@(
		'PowerToys.Common.UI.dll',
		'PowerToys.ManagedCommon.dll',
		'PowerToys.Settings.UI.Lib.dll',
		'Wox.Infrastructure.dll',
		'Wox.Plugin.dll',
		'Wpf.Ui.dll',
		'Microsoft.Xaml.Behaviors.dll'
	) | ForEach-Object {
		New-Item ./Lib/$_ -ItemType SymbolicLink -Value "$ptPath\$_"
	}
}

Pop-Location
