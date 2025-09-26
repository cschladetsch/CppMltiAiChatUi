#!/usr/bin/env python3
"""
Setup script for CsharpUAI project
Handles submodules initialization and other setup tasks
"""

import os
import sys
import subprocess
import argparse
from pathlib import Path


def run_command(cmd, cwd=None, check=True):
    """Run a command and handle errors"""
    print(f"Running: {' '.join(cmd) if isinstance(cmd, list) else cmd}")
    try:
        result = subprocess.run(
            cmd,
            cwd=cwd,
            check=check,
            capture_output=True,
            text=True,
            shell=True if isinstance(cmd, str) else False
        )
        if result.stdout:
            print(result.stdout)
        return result
    except subprocess.CalledProcessError as e:
        print(f"Error running command: {e}")
        if e.stderr:
            print(f"stderr: {e.stderr}")
        if check:
            sys.exit(1)
        return e


def init_submodules():
    """Initialize and update git submodules"""
    print("Initializing git submodules...")

    # Check if we have any submodules
    if not os.path.exists('.gitmodules'):
        print("No .gitmodules file found, checking for orphaned submodules...")

        # Check for orphaned serilog submodule
        serilog_path = Path("MultipleAIApp/serilog")
        if serilog_path.exists() and not serilog_path.joinpath(".git").exists():
            print("Found orphaned serilog directory, removing...")
            import shutil
            shutil.rmtree(serilog_path, ignore_errors=True)

        print("No submodules to initialize.")
        return

    # Initialize submodules
    run_command(["git", "submodule", "init"])
    run_command(["git", "submodule", "update", "--recursive"])

    print("Submodules initialized successfully!")


def restore_packages():
    """Restore NuGet packages for all solutions"""
    print("Restoring NuGet packages...")

    solutions = [
        "MultiLLM.sln",
        "MultipleAIApp/MultipleAIApp.sln"
    ]

    for solution in solutions:
        if os.path.exists(solution):
            print(f"Restoring packages for {solution}...")
            run_command(["dotnet", "restore", solution])
        else:
            print(f"Solution {solution} not found, skipping...")


def setup_development_environment():
    """Setup development environment"""
    print("Setting up development environment...")

    # Ensure .NET SDK is available
    result = run_command(["dotnet", "--version"], check=False)
    if result.returncode != 0:
        print("ERROR: .NET SDK not found. Please install .NET 8 SDK.")
        print("Download from: https://dotnet.microsoft.com/download")
        sys.exit(1)

    print("Development environment setup complete!")


def main():
    """Main setup function"""
    parser = argparse.ArgumentParser(description="Setup CsharpUAI project")
    parser.add_argument("--skip-submodules", action="store_true",
                       help="Skip submodule initialization")
    parser.add_argument("--skip-restore", action="store_true",
                       help="Skip NuGet package restore")

    args = parser.parse_args()

    print("=== CsharpUAI Project Setup ===")

    # Change to project root directory
    project_root = Path(__file__).parent
    os.chdir(project_root)

    try:
        # Setup development environment
        setup_development_environment()

        # Initialize submodules (if any)
        if not args.skip_submodules:
            init_submodules()

        # Restore NuGet packages
        if not args.skip_restore:
            restore_packages()

        print("\n=== Setup Complete! ===")
        print("You can now build the project using:")
        print("  python build.py")
        print("  python build.py --clean")
        print("  python build.py --run")

    except KeyboardInterrupt:
        print("\nSetup interrupted by user.")
        sys.exit(1)
    except Exception as e:
        print(f"Setup failed: {e}")
        sys.exit(1)


if __name__ == "__main__":
    main()