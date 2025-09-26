#!/usr/bin/env python3
"""
Build script for MultipleAIApp
Usage: python b.py [--clean] [--run]
"""

import argparse
import subprocess
import sys
import os

def main():
    parser = argparse.ArgumentParser(description='Build MultipleAIApp')
    parser.add_argument('--clean', action='store_true', help='Clean before building')
    parser.add_argument('--run', action='store_true', help='Run the application after building')

    args = parser.parse_args()

    try:
        # Change to the directory containing the solution file
        script_dir = os.path.dirname(os.path.abspath(__file__))
        os.chdir(script_dir)

        if args.clean:
            print("Cleaning solution...")
            result = subprocess.run(['dotnet', 'clean', 'MultipleAIApp.sln'], check=True)

        print("Building solution...")
        result = subprocess.run(['dotnet', 'build', 'MultipleAIApp.sln'], check=True)
        print("Build completed successfully!")

        if args.run:
            print("Running application...")
            result = subprocess.run(['dotnet', 'run', '--project', 'MultipleAIApp/MultipleAIApp.csproj', '--framework', 'net8.0-desktop'], check=True)

    except subprocess.CalledProcessError as e:
        print(f"Command failed with exit code {e.returncode}")
        sys.exit(e.returncode)
    except KeyboardInterrupt:
        print("\nBuild interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == '__main__':
    main()